using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.StoreRoom;

public sealed class StoreRoomService(IDbConnectionFactory connectionFactory)
{
    private static readonly HashSet<string> AllowedMovements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Stock In", "Reserve", "Release Reservation", "Issue", "Return", "Adjustment", "Stock Correction -", "Write-Off", "Dispose Quarantined"
    };

    public async Task<IReadOnlyList<StoreItemDto>> GetItemsAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<StoreItemDto>(new CommandDefinition("""
            SELECT Id, Name, Category, Sku, Location, QuantityOnHand, ReservedQuantity, QuarantinedQuantity,
                   MinimumStock, IsAssetTracked, IsActive
            FROM StoreItems
            WHERE IsActive = 1
            ORDER BY Name;
            """, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<StoreMovementDto>> GetMovementsAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<StoreMovementDto>(new CommandDefinition("""
            SELECT m.Id, m.StoreItemId, i.Name AS ItemName, m.TicketId, r.TicketNumber,
                   m.MovementType, m.Quantity, m.Reference, m.Notes, m.PerformedBy, m.PerformedAtUtc
            FROM StoreMovements m
            INNER JOIN StoreItems i ON i.Id = m.StoreItemId
            LEFT JOIN AssetRequests r ON r.Id = m.TicketId
            ORDER BY m.PerformedAtUtc DESC, m.Id DESC
            LIMIT 200;
            """, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<StoreLinkedAssetDto>> GetAssetInventoryAsync(CancellationToken ct = default)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        var assets = await db.QueryAsync<StoreLinkedAssetDto>(new CommandDefinition("""
            SELECT a.Id,a.AssetTag,a.SerialNumber,a.Category,a.Make,a.Model,a.Status,a.Condition,
                   l.StoreItemId,s.Name AS StoreItemName,
                   COALESCE(l.PhysicallyInStore,0) AS PhysicallyInStore,
                   trim(COALESCE(e.FirstName,'') || ' ' || COALESCE(e.LastName,'')) AS AssignedEmployee
            FROM Assets a
            LEFT JOIN StoreAssetLinks l ON l.AssetId=a.Id
            LEFT JOIN StoreItems s ON s.Id=l.StoreItemId
            LEFT JOIN AssetAssignments aa ON aa.AssetId=a.Id AND aa.Status='Assigned'
            LEFT JOIN Employees e ON e.Id=aa.EmployeeId
            ORDER BY (CASE WHEN a.Condition='Damaged' OR a.Status IN ('In Repair','Pending Write-Off','Approved for Write-Off') THEN 0 ELSE 1 END), a.AssetTag
            """, cancellationToken:ct));
        return assets.AsList();
    }

    public async Task LinkAssetAsync(int storeItemId, LinkStoreAssetDto dto, string actor, CancellationToken ct = default)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var storeItem = await db.QuerySingleOrDefaultAsync<StoreItemDto>(new CommandDefinition("""
            SELECT Id,Name,Category,Sku,Location,QuantityOnHand,ReservedQuantity,QuarantinedQuantity,MinimumStock,IsAssetTracked,IsActive
            FROM StoreItems WHERE Id=@storeItemId AND IsActive=1
            """,new {storeItemId},tx,cancellationToken:ct));
        if(storeItem is null || !storeItem.IsAssetTracked)
            throw new InvalidOperationException("Select an active asset-tracked Store Room item.");
        var asset = await db.QuerySingleOrDefaultAsync<StoreLinkedAssetDto>(new CommandDefinition("""
            SELECT Id,AssetTag,Condition,Status FROM Assets WHERE Id=@id
            """,new {id=dto.AssetId},tx,cancellationToken:ct));
        if(asset is null || asset.Status.Equals("Disposed",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Select a valid asset that is not disposed.");
        if(await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM StoreAssetLinks WHERE AssetId=@id",
            new {id=dto.AssetId},tx,cancellationToken:ct))>0)
            throw new InvalidOperationException("Asset is already linked to a Store Room item.");
        bool usable = asset.Status.Equals("Available",StringComparison.OrdinalIgnoreCase) &&
            !asset.Condition.Equals("Damaged",StringComparison.OrdinalIgnoreCase) &&
            !asset.Condition.Equals("Obsolete",StringComparison.OrdinalIgnoreCase);
        if (asset.Status.Equals("Assigned",StringComparison.OrdinalIgnoreCase) && dto.PhysicallyInStore)
            throw new InvalidOperationException("An assigned asset is still in employee custody. Process the return before confirming it in Store Room.");
        if (usable && !dto.PhysicallyInStore)
            throw new InvalidOperationException("Available assets must be physically confirmed in the Store Room before linking.");
        if (usable)
        {
            var counted = await db.ExecuteScalarAsync<int>(new CommandDefinition("""
                SELECT COUNT(*) FROM StoreAssetLinks l
                JOIN Assets a ON a.Id=l.AssetId
                WHERE l.StoreItemId=@storeItemId AND l.PhysicallyInStore=1 AND a.Status='Available'
                """,new {storeItemId},tx,cancellationToken:ct));
            if(storeItem.QuantityOnHand-storeItem.ReservedQuantity <= counted)
                throw new InvalidOperationException("No unlinked available unit remains for this stock item. Receive/reconcile stock first.");
        }
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StoreAssetLinks(AssetId,StoreItemId,PhysicallyInStore,LinkedAtUtc,LinkedBy)
            VALUES(@assetId,@storeItemId,@inStore,@now,@actor)
            """,new {assetId=dto.AssetId,storeItemId,inStore=dto.PhysicallyInStore,now=DateTime.UtcNow,actor},
            tx,cancellationToken:ct));
        // Physical damaged items are recorded separately from usable stock.
        if(dto.PhysicallyInStore && !usable &&
            (asset.Condition.Equals("Damaged",StringComparison.OrdinalIgnoreCase) ||
             asset.Condition.Equals("Obsolete",StringComparison.OrdinalIgnoreCase) ||
             asset.Status is "In Repair" or "Pending Write-Off" or "Approved for Write-Off"))
        {
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreItems SET QuarantinedQuantity=QuarantinedQuantity+1,UpdatedAtUtc=@now WHERE Id=@storeItemId
                """,new {storeItemId,now=DateTime.UtcNow},tx,cancellationToken:ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO StoreMovements(StoreItemId,AssetId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
                VALUES(@storeItemId,@assetId,'Quarantine',1,@ref,'Existing damaged asset reconciled to physical store',@actor,@now)
                """,new {storeItemId,assetId=dto.AssetId,@ref=asset.AssetTag,actor,now=DateTime.UtcNow},tx,cancellationToken:ct));
        }
        await tx.CommitAsync(ct);
    }

    public async Task<StoreItemDto> CreateItemAsync(CreateStoreItemDto dto, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Category))
            throw new ArgumentException("Item name and category are required.");
        if (dto.OpeningQuantity < 0 || dto.MinimumStock < 0)
            throw new ArgumentException("Quantities cannot be negative.");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO StoreItems
                (Name, Category, Sku, Location, QuantityOnHand, ReservedQuantity, MinimumStock, IsAssetTracked, IsActive, CreatedAtUtc)
            VALUES
                (@Name, @Category, @Sku, @Location, @OpeningQuantity, 0, @MinimumStock, @IsAssetTracked, 1, @Now);
            SELECT last_insert_rowid();
            """, new {
                Name = dto.Name.Trim(), Category = dto.Category.Trim(), Sku = Clean(dto.Sku), Location = Clean(dto.Location),
                dto.OpeningQuantity, dto.MinimumStock, dto.IsAssetTracked, Now = now
            }, tx, cancellationToken: ct));

        if (dto.OpeningQuantity > 0)
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO StoreMovements
                    (StoreItemId, TicketId, MovementType, Quantity, Reference, Notes, PerformedBy, PerformedAtUtc)
                VALUES
                    (@StoreItemId, NULL, 'Stock In', @Quantity, 'Opening stock', 'Initial store quantity', @Actor, @Now);
                """, new { StoreItemId = id, Quantity = dto.OpeningQuantity, Actor = actor, Now = now }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return await GetItemAsync(id, ct) ?? throw new InvalidOperationException("Store item was not created.");
    }

    public async Task ApplyMovementAsync(int itemId, StoreMovementRequest request, string actor, CancellationToken ct = default)
    {
        if (!AllowedMovements.Contains(request.MovementType)) throw new ArgumentException("Unsupported movement type.");
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        var item = await connection.QuerySingleOrDefaultAsync<StoreItemDto>(new CommandDefinition("""
            SELECT Id, Name, Category, Sku, Location, QuantityOnHand, ReservedQuantity, MinimumStock, IsAssetTracked, IsActive
            FROM StoreItems WHERE Id = @Id AND IsActive = 1;
            """, new { Id = itemId }, tx, cancellationToken: ct)) ?? throw new KeyNotFoundException("Store item not found.");

        if(item.IsAssetTracked && (request.MovementType.Equals("Issue",StringComparison.OrdinalIgnoreCase) ||
             request.MovementType.Equals("Return",StringComparison.OrdinalIgnoreCase) ||
             request.MovementType.Equals("Write-Off",StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Asset-tracked stock must be issued, returned or disposed through Asset Assignments / Write-Offs to keep inventory and asset history synchronised.");

        var onHand = item.QuantityOnHand;
        var reserved = item.ReservedQuantity;
        var quarantined = item.QuarantinedQuantity;
        var qty = request.Quantity;

        switch (request.MovementType)
        {
            case "Stock In": onHand += qty; break;
            case "Reserve":
                if (onHand - reserved < qty) throw new InvalidOperationException("Not enough available stock to reserve.");
                reserved += qty;
                break;
            case "Release Reservation":
                if (reserved < qty) throw new InvalidOperationException("Cannot release more than the reserved quantity.");
                reserved -= qty;
                break;
            case "Issue":
                if (reserved >= qty) reserved -= qty;
                else if (onHand - reserved < qty) throw new InvalidOperationException("Not enough available stock to issue.");
                onHand -= qty;
                break;
            case "Return": onHand += qty; break;
            case "Write-Off":
                if (onHand - reserved < qty) throw new InvalidOperationException("Not enough unreserved stock to write off.");
                onHand -= qty;
                break;
            case "Adjustment":
                onHand += qty;
                break;
            case "Dispose Quarantined":
                if (item.IsAssetTracked)
                    throw new InvalidOperationException("Quarantined tagged assets must be disposed through Write-Offs & E-Waste.");
                if (quarantined < qty)
                    throw new InvalidOperationException("Cannot dispose more units than physically quarantined.");
                quarantined -= qty;
                break;
            case "Stock Correction -":
                if (onHand - reserved < qty)
                    throw new InvalidOperationException("Stock correction cannot reduce reserved or unavailable units.");
                if (item.IsAssetTracked)
                {
                    var linkedUsable = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
                        SELECT COUNT(*) FROM StoreAssetLinks l JOIN Assets a ON a.Id=l.AssetId
                        WHERE l.StoreItemId=@itemId AND l.PhysicallyInStore=1 AND a.Status='Available'
                        """, new { itemId }, tx, cancellationToken: ct));
                    if (onHand - qty < linkedUsable)
                        throw new InvalidOperationException("Cannot remove stock already reconciled to available asset tags.");
                }
                onHand -= qty;
                break;
        }

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems
            SET QuantityOnHand = @OnHand, ReservedQuantity = @Reserved, QuarantinedQuantity = @Quarantined, UpdatedAtUtc = @Now
            WHERE Id = @Id;
            """, new { Id = itemId, OnHand = onHand, Reserved = reserved, Quarantined = quarantined, Now = now }, tx, cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StoreMovements
                (StoreItemId, TicketId, MovementType, Quantity, Reference, Notes, PerformedBy, PerformedAtUtc)
            VALUES
                (@StoreItemId, @TicketId, @MovementType, @Quantity, @Reference, @Notes, @PerformedBy, @Now);
            """, new {
                StoreItemId = itemId, request.TicketId, request.MovementType, request.Quantity,
                Reference = Clean(request.Reference), Notes = Clean(request.Notes), PerformedBy = actor, Now = now
            }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    private async Task<StoreItemDto?> GetItemAsync(int id, CancellationToken ct)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<StoreItemDto>(new CommandDefinition("""
            SELECT Id, Name, Category, Sku, Location, QuantityOnHand, ReservedQuantity, MinimumStock, IsAssetTracked, IsActive
            FROM StoreItems WHERE Id = @Id;
            """, new { Id = id }, cancellationToken: ct));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
