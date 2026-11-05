using System.Data.Common;
using Dapper;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Deliveries;

/// <summary>
/// Equipment delivery is the source of dispatch state. All stock, custody, ticket and
/// event changes happen in the same SQLite transaction.
/// </summary>
public sealed class EquipmentDeliveryService(IDbConnectionFactory connections)
{
    private sealed class TicketRow
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Status { get; set; } = "";
        public int Quantity { get; set; }
    }

    private sealed class StockRow
    {
        public int Id { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsAssetTracked { get; set; }
        public bool IsActive { get; set; }
    }

    private const string DetailQuery = """
        SELECT d.*,t.TicketNumber,s.Name AS StoreItemName,a.AssetTag
        FROM EquipmentDeliveries d
        INNER JOIN AssetRequests t ON t.Id=d.TicketId
        LEFT JOIN StoreItems s ON s.Id=d.StoreItemId
        LEFT JOIN Assets a ON a.Id=d.AssetId
        """;

    public async Task<EquipmentDeliveryDto?> GetAsync(int ticketId, CancellationToken ct = default)
    {
        await using var db=connections.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<EquipmentDeliveryDto>(
            new CommandDefinition(DetailQuery+" WHERE d.TicketId=@ticketId",new{ticketId},cancellationToken:ct));
    }

    public async Task<IReadOnlyList<EquipmentDeliveryDto>> GetOpenAsync(CancellationToken ct = default)
    {
        await using var db=connections.CreateConnection();
        var rows=await db.QueryAsync<EquipmentDeliveryDto>(new CommandDefinition(
            DetailQuery+" WHERE d.Status NOT IN ('Done','Returned to Sender') ORDER BY d.UpdatedAtUtc DESC",
            cancellationToken:ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<EquipmentDeliveryEventDto>> HistoryAsync(int ticketId, CancellationToken ct = default)
    {
        await using var db=connections.CreateConnection();
        var rows=await db.QueryAsync<EquipmentDeliveryEventDto>(new CommandDefinition("""
            SELECT e.* FROM EquipmentDeliveryEvents e
            JOIN EquipmentDeliveries d ON d.Id=e.DeliveryId
            WHERE d.TicketId=@ticketId ORDER BY e.Id
            """,new{ticketId},cancellationToken:ct));
        return rows.AsList();
    }

    public async Task<EquipmentDeliveryDto> PrepareAsync(int ticketId,PrepareEquipmentDeliveryDto request,string actor,CancellationToken ct=default)
    {
        if(request.StoreItemId<1 || string.IsNullOrWhiteSpace(request.RecipientName))
            throw new ArgumentException("Store item and receiving employee are required.");
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var ticket=await db.QuerySingleOrDefaultAsync<TicketRow>(new CommandDefinition(
            "SELECT Id,TicketNumber,RequestType,Branch,Quantity,Status FROM AssetRequests WHERE Id=@ticketId",
            new{ticketId},tx,cancellationToken:ct))
            ?? throw new KeyNotFoundException("Equipment request not found.");
        if(!ticket.RequestType.Equals("Equipment",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Delivery applies only to equipment requests.");
        if(ticket.Status is not ("Assigned" or "In Progress"))
            throw new InvalidOperationException("Assign and begin processing the equipment ticket before preparing delivery.");
        if(ticket.Quantity<=0) throw new InvalidOperationException("Ticket quantity must be positive.");
        if(!request.DeliveryRequired && !ticket.Branch.Equals("Phoenix",StringComparison.OrdinalIgnoreCase) &&
           string.IsNullOrWhiteSpace(request.LocalHandoverReason))
            throw new ArgumentException("Explain why a remote branch request will be handed over locally.");
        if(await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM EquipmentDeliveries WHERE TicketId=@ticketId",new{ticketId},tx,cancellationToken:ct))>0)
            throw new InvalidOperationException("This request already has a delivery record.");

        var item=await db.QuerySingleOrDefaultAsync<StockRow>(new CommandDefinition("""
            SELECT Id,QuantityOnHand,ReservedQuantity,IsAssetTracked,IsActive
            FROM StoreItems WHERE Id=@id
            """,new{id=request.StoreItemId},tx,cancellationToken:ct));
        if(item is null || !item.IsActive)throw new InvalidOperationException("Select an active Store Room item.");
        if(item.QuantityOnHand-item.ReservedQuantity<ticket.Quantity)
            throw new InvalidOperationException("Insufficient available Store Room stock for this request.");
        if(item.IsAssetTracked)
        {
            if(ticket.Quantity!=1 || !request.AssetId.HasValue)
                throw new InvalidOperationException("Select one exact asset tag for each asset-tracked delivery. Split multi-unit equipment requests before delivery.");
            var eligible=await db.ExecuteScalarAsync<int>(new CommandDefinition("""
                SELECT COUNT(*) FROM StoreAssetLinks l
                JOIN Assets a ON a.Id=l.AssetId
                WHERE l.AssetId=@assetId AND l.StoreItemId=@itemId AND l.PhysicallyInStore=1
                  AND a.Status='Available' AND a.Condition NOT IN ('Damaged','Obsolete')
                """,new{assetId=request.AssetId.Value,itemId=item.Id},tx,cancellationToken:ct));
            if(eligible!=1)throw new InvalidOperationException("Selected asset is not available and physically verified in this Store Room item.");
        }
        else if(request.AssetId.HasValue)
            throw new ArgumentException("Individual asset tags can only be selected for asset-tracked stock.");

        var now=DateTime.UtcNow;
        var reserved=await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems SET ReservedQuantity=ReservedQuantity+@qty,UpdatedAtUtc=@now
            WHERE Id=@id AND IsActive=1 AND QuantityOnHand-ReservedQuantity>=@qty
            """,new{id=item.Id,qty=ticket.Quantity,now},tx,cancellationToken:ct));
        if(reserved!=1)throw new InvalidOperationException("Not enough stock to reserve.");

        var status=request.DeliveryRequired?"Waiting for Driver Pickup":"Awaiting Local Handover";
        var id=await db.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO EquipmentDeliveries(TicketId,StoreItemId,AssetId,DestinationBranch,DeliveryRequired,
                RecipientName,Quantity,Status,PreparedAtUtc,CreatedBy,CreatedAtUtc,UpdatedAtUtc)
            VALUES(@ticketId,@storeItemId,@assetId,@branch,@delivery,@recipient,@qty,@status,@now,@actor,@now,@now);
            SELECT last_insert_rowid();
            """,new{ticketId,storeItemId=item.Id,assetId=request.AssetId,branch=ticket.Branch,
                delivery=request.DeliveryRequired,recipient=request.RecipientName.Trim(),qty=ticket.Quantity,status,actor,now},
            tx,cancellationToken:ct));
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StoreMovements(StoreItemId,TicketId,AssetId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
            VALUES(@storeItemId,@ticketId,@assetId,'Reserve',@qty,@ref,@notes,@actor,@now)
            """,new{storeItemId=item.Id,ticketId,assetId=request.AssetId,qty=ticket.Quantity,
                @ref=ticket.TicketNumber,notes="Reserved for approved equipment delivery",actor,now},tx,cancellationToken:ct));
        await StageAsync(db,tx,id,ticketId,ticket.Status,status,actor,
            request.DeliveryRequired?"Stock reserved; awaiting driver pickup.":"Stock reserved for local recipient handover.",now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    public async Task<EquipmentDeliveryDto> PickupAsync(int ticketId,PickupEquipmentDeliveryDto request,string actor,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(request.DriverName))throw new ArgumentException("Driver or courier name is required.");
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var delivery=await RequireAsync(db,tx,ticketId,ct);
        if(!delivery.DeliveryRequired || delivery.Status!="Waiting for Driver Pickup")
            throw new InvalidOperationException("Driver pickup is permitted only for prepared deliveries awaiting collection.");
        await IssueReservedAsync(db,tx,delivery,actor,ct);
        var now=DateTime.UtcNow;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EquipmentDeliveries SET Status='In Transit',DriverName=@driver,TrackingReference=@reference,
                PickedUpAtUtc=@now,UpdatedAtUtc=@now WHERE Id=@id
            """,new{id=delivery.Id,driver=request.DriverName.Trim(),
                reference=Clean(request.TrackingReference),now},tx,cancellationToken:ct));
        await StageAsync(db,tx,delivery.Id,ticketId,delivery.Status,"In Transit",actor,
            $"Collected by {request.DriverName.Trim()}; {request.TrackingReference}",now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    public async Task<EquipmentDeliveryDto> ArrivedAsync(int ticketId,string actor,CancellationToken ct=default)
    {
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var delivery=await RequireAsync(db,tx,ticketId,ct);
        if(delivery.Status!="In Transit")throw new InvalidOperationException("Only in-transit shipments can be marked delivered to the branch.");
        var now=DateTime.UtcNow;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EquipmentDeliveries SET Status='Awaiting Receipt Confirmation',UpdatedAtUtc=@now WHERE Id=@id
            """,new{id=delivery.Id,now},tx,cancellationToken:ct));
        await StageAsync(db,tx,delivery.Id,ticketId,delivery.Status,"Awaiting Receipt Confirmation",actor,
            "Driver reports delivery; awaiting named branch recipient confirmation.",now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    public async Task<EquipmentDeliveryDto> ConfirmAsync(int ticketId,ConfirmEquipmentReceiptDto request,string actor,CancellationToken ct=default)
    {
        if(request.Quantity<1 || string.IsNullOrWhiteSpace(request.ReceivedBy) ||
           string.IsNullOrWhiteSpace(request.ReceiptReference))
            throw new ArgumentException("Received quantity, receiving person and receipt reference are required.");
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var delivery=await RequireAsync(db,tx,ticketId,ct);
        if(delivery.Status is not ("Awaiting Receipt Confirmation" or "Partially Received" or "Awaiting Local Handover"))
            throw new InvalidOperationException("Pickup or branch arrival must be recorded before receipt confirmation.");
        if(delivery.ReceivedQuantity+request.Quantity>delivery.Quantity)
            throw new InvalidOperationException("Cannot confirm more items than were dispatched.");
        if(delivery.Status=="Awaiting Local Handover")
        {
            if(request.Quantity!=delivery.Quantity)
                throw new InvalidOperationException("Local handover must be confirmed in full.");
            await IssueReservedAsync(db,tx,delivery,actor,ct);
        }
        var total=delivery.ReceivedQuantity+request.Quantity;
        var done=total==delivery.Quantity;
        var status=done?"Done":"Partially Received";
        var now=DateTime.UtcNow;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EquipmentDeliveries SET ReceivedQuantity=@total,ReceivedBy=@receivedBy,
                ReceiptReference=@reference,ReceivedAtUtc=@now,
                CompletedAtUtc=CASE WHEN @done THEN @now ELSE NULL END,
                Status=@status,UpdatedAtUtc=@now WHERE Id=@id
            """,new{id=delivery.Id,total,receivedBy=request.ReceivedBy.Trim(),
                reference=request.ReceiptReference.Trim(),done,status,now},tx,cancellationToken:ct));
        if(done && delivery.AssetId.HasValue)
        {
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE Assets SET Status='Delivered',
                    BranchId=COALESCE((SELECT Id FROM Branches WHERE Name=@branch LIMIT 1),BranchId),
                    UpdatedAtUtc=@now WHERE Id=@assetId
                """,new{branch=delivery.DestinationBranch,assetId=delivery.AssetId.Value,now},tx,cancellationToken:ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc)
                VALUES(@assetId,'Delivered',@detail,@actor,@now)
                """,new{assetId=delivery.AssetId.Value,
                    detail=$"Received at {delivery.DestinationBranch} by {request.ReceivedBy.Trim()}; proof {request.ReceiptReference.Trim()}",
                    actor,now},tx,cancellationToken:ct));
        }
        await StageAsync(db,tx,delivery.Id,ticketId,delivery.Status,status,actor,
            $"{request.Quantity} unit(s) confirmed by {request.ReceivedBy.Trim()}; proof {request.ReceiptReference.Trim()}. {Clean(request.Notes)}",now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    public async Task<EquipmentDeliveryDto> FailAsync(int ticketId,DeliveryExceptionDto request,string actor,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(request.Notes))throw new ArgumentException("Explain why delivery failed.");
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var delivery=await RequireAsync(db,tx,ticketId,ct);
        if(delivery.Status is not ("In Transit" or "Awaiting Receipt Confirmation" or "Partially Received"))
            throw new InvalidOperationException("Only dispatched equipment can be reported as failed delivery.");
        var now=DateTime.UtcNow;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EquipmentDeliveries SET Status='Delivery Failed',ExceptionNotes=@notes,
                UpdatedAtUtc=@now WHERE Id=@id
            """,new{id=delivery.Id,notes=request.Notes.Trim(),now},tx,cancellationToken:ct));
        await StageAsync(db,tx,delivery.Id,ticketId,delivery.Status,"Delivery Failed",actor,request.Notes.Trim(),now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    public async Task<EquipmentDeliveryDto> ReturnedAsync(int ticketId,DeliveryExceptionDto request,string actor,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(request.Notes))throw new ArgumentException("Explain the return and confirm physical custody.");
        await using var db=connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var delivery=await RequireAsync(db,tx,ticketId,ct);
        if(delivery.Status!="Delivery Failed")
            throw new InvalidOperationException("First mark the delivery as failed before recording return to sender.");
        if(delivery.AssetId.HasValue && delivery.ReceivedQuantity>0)
            throw new InvalidOperationException("A tagged asset already confirmed received cannot also be returned.");
        var outstanding=delivery.Quantity-delivery.ReceivedQuantity;
        if(outstanding<=0)throw new InvalidOperationException("No outstanding stock to return.");
        var now=DateTime.UtcNow;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems SET QuantityOnHand=QuantityOnHand+@qty,UpdatedAtUtc=@now WHERE Id=@id
            """,new{id=delivery.StoreItemId,qty=outstanding,now},tx,cancellationToken:ct));
        if(delivery.AssetId.HasValue)
        {
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreAssetLinks SET PhysicallyInStore=1 WHERE AssetId=@assetId
                """,new{assetId=delivery.AssetId.Value},tx,cancellationToken:ct));
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE Assets SET Status='Available',UpdatedAtUtc=@now WHERE Id=@assetId
                """,new{assetId=delivery.AssetId.Value,now},tx,cancellationToken:ct));
        }
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StoreMovements(StoreItemId,TicketId,AssetId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
            VALUES(@itemId,@ticketId,@assetId,'Return',@qty,@reference,@notes,@actor,@now)
            """,new{itemId=delivery.StoreItemId,ticketId,assetId=delivery.AssetId,qty=outstanding,
                reference=delivery.TicketNumber,notes=request.Notes.Trim(),actor,now},tx,cancellationToken:ct));
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EquipmentDeliveries SET Status='Returned to Sender',ExceptionNotes=@notes,UpdatedAtUtc=@now
            WHERE Id=@id
            """,new{id=delivery.Id,notes=request.Notes.Trim(),now},tx,cancellationToken:ct));
        await StageAsync(db,tx,delivery.Id,ticketId,delivery.Status,"Returned to Sender",actor,
            $"{outstanding} undelivered item(s) physically returned. {request.Notes.Trim()}",now,ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(ticketId,ct))!;
    }

    private static async Task IssueReservedAsync(DbConnection db,DbTransaction tx,EquipmentDeliveryDto delivery,string actor,CancellationToken ct)
    {
        var now=DateTime.UtcNow;
        var released=await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems SET ReservedQuantity=ReservedQuantity-@qty,UpdatedAtUtc=@now
            WHERE Id=@itemId AND ReservedQuantity>=@qty
            """,new{itemId=delivery.StoreItemId,qty=delivery.Quantity,now},tx,cancellationToken:ct));
        if(released!=1)throw new InvalidOperationException("Reservation missing; contact IT for reconciliation.");
        if(delivery.AssetId.HasValue)
        {
            await LinkedAssetInventory.IssueAsync(db,tx,delivery.AssetId.Value,actor,ct);
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE Assets SET Status='In Transit',UpdatedAtUtc=@now WHERE Id=@id
                """,new{id=delivery.AssetId.Value,now},tx,cancellationToken:ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc)
                VALUES(@id,'Dispatched',@notes,@actor,@now)
                """,new{id=delivery.AssetId.Value,notes=$"Dispatched for ticket {delivery.TicketNumber}",actor,now},tx,cancellationToken:ct));
        }
        else
        {
            var removed=await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreItems SET QuantityOnHand=QuantityOnHand-@qty,UpdatedAtUtc=@now
                WHERE Id=@itemId AND QuantityOnHand-ReservedQuantity>=@qty
                """,new{itemId=delivery.StoreItemId,qty=delivery.Quantity,now},tx,cancellationToken:ct));
            if(removed!=1)throw new InvalidOperationException("Not enough usable stock to dispatch.");
        }
        if(!delivery.AssetId.HasValue)
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO StoreMovements(StoreItemId,TicketId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
                VALUES(@itemId,@ticketId,'Issue',@qty,@reference,'Dispatched from IT Store',@actor,@now)
                """,new{itemId=delivery.StoreItemId,ticketId=delivery.TicketId,qty=delivery.Quantity,
                    reference=delivery.TicketNumber,actor,now},tx,cancellationToken:ct));
        else
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreMovements SET TicketId=@ticketId
                WHERE Id=(SELECT Id FROM StoreMovements WHERE AssetId=@assetId AND MovementType='Issue' ORDER BY Id DESC LIMIT 1)
                """,new{ticketId=delivery.TicketId,assetId=delivery.AssetId},tx,cancellationToken:ct));
    }

    private static async Task<EquipmentDeliveryDto> RequireAsync(DbConnection db,DbTransaction tx,int ticketId,CancellationToken ct)
        => await db.QuerySingleOrDefaultAsync<EquipmentDeliveryDto>(new CommandDefinition(
            DetailQuery+" WHERE d.TicketId=@ticketId",new{ticketId},tx,cancellationToken:ct))
           ?? throw new KeyNotFoundException("Delivery record not found.");

    private static async Task StageAsync(DbConnection db,DbTransaction tx,int deliveryId,int ticketId,
        string from,string to,string actor,string detail,DateTime now,CancellationToken ct)
    {
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO EquipmentDeliveryEvents(DeliveryId,FromStatus,ToStatus,Details,PerformedBy,PerformedAtUtc)
            VALUES(@deliveryId,@from,@to,@detail,@actor,@now)
            """,new{deliveryId,from,to,detail,actor,now},tx,cancellationToken:ct));
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests SET Status=@to,
                ResolvedAtUtc=CASE WHEN @to='Done' THEN @now ELSE ResolvedAtUtc END,
                CompletedAtUtc=CASE WHEN @to='Done' THEN @now ELSE CompletedAtUtc END,
                LastUpdatedAtUtc=@now WHERE Id=@ticketId
            """,new{ticketId,to,now},tx,cancellationToken:ct));
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO TicketStatusHistory(TicketId,FromStatus,ToStatus,ChangedBy,ChangedByName,Comment,PublicVisible,ChangedAtUtc)
            VALUES(@ticketId,@from,@to,@actor,@actor,@detail,1,@now)
            """,new{ticketId,from,to,actor,detail,now},tx,cancellationToken:ct));
    }

    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
