using System.Data.Common;
using Dapper;

namespace RoyalDesk.Api.StoreRoom;

/// <summary>
/// Synchronises linked serialised assets with the store ledger inside the caller's transaction.
/// Unlinked legacy assets are left unchanged until IT reconciles them explicitly.
/// </summary>
internal static class LinkedAssetInventory
{
    private sealed class LinkedRow
    {
        public int StoreItemId { get; set; }
        public bool PhysicallyInStore { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public string Condition { get; set; } = "";
        public string Status { get; set; } = "";
    }

    private static async Task<LinkedRow?> GetAsync(DbConnection db, DbTransaction tx, long assetId, CancellationToken ct)
        => await db.QuerySingleOrDefaultAsync<LinkedRow>(new CommandDefinition("""
            SELECT l.StoreItemId, l.PhysicallyInStore, s.QuantityOnHand, s.ReservedQuantity,
                   a.Condition, a.Status
            FROM StoreAssetLinks l
            JOIN StoreItems s ON s.Id=l.StoreItemId
            JOIN Assets a ON a.Id=l.AssetId
            WHERE l.AssetId=@assetId
            """, new { assetId }, tx, cancellationToken: ct));

    private static async Task MovementAsync(DbConnection db, DbTransaction tx, long assetId, int storeItemId,
        string movementType, string description, string actor, CancellationToken ct)
    {
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StoreMovements(StoreItemId,AssetId,TicketId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
            VALUES(@storeItemId,@assetId,NULL,@movementType,1,@reference,@description,@actor,@now)
            """, new {storeItemId,assetId,movementType,reference=$"Asset #{assetId}",description,actor,now=DateTime.UtcNow},
            tx,cancellationToken:ct));
    }

    public static async Task IssueAsync(DbConnection db, DbTransaction tx, long assetId, string actor, CancellationToken ct)
    {
        var link = await GetAsync(db, tx, assetId, ct);
        if (link is null) return;
        if (!link.PhysicallyInStore || link.QuantityOnHand-link.ReservedQuantity < 1)
            throw new InvalidOperationException("Linked asset is not available in Store Room. Reconcile the stock before assigning it.");
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems SET QuantityOnHand=QuantityOnHand-1,UpdatedAtUtc=@now WHERE Id=@id
            """,new {id=link.StoreItemId,now=DateTime.UtcNow},tx,cancellationToken:ct));
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreAssetLinks SET PhysicallyInStore=0 WHERE AssetId=@assetId
            """,new {assetId},tx,cancellationToken:ct));
        await MovementAsync(db,tx,assetId,link.StoreItemId,"Issue","Issued to employee through Asset Assignments",actor,ct);
    }

    public static async Task ReturnAsync(DbConnection db, DbTransaction tx, long assetId, bool damaged, string actor, CancellationToken ct)
    {
        var link = await GetAsync(db, tx, assetId, ct);
        if (link is null) return;
        if (link.PhysicallyInStore) throw new InvalidOperationException("Asset is already recorded as physically in Store Room.");
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreAssetLinks SET PhysicallyInStore=1 WHERE AssetId=@assetId
            """,new {assetId},tx,cancellationToken:ct));
        if (!damaged)
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreItems SET QuantityOnHand=QuantityOnHand+1,UpdatedAtUtc=@now WHERE Id=@id
                """,new {id=link.StoreItemId,now=DateTime.UtcNow},tx,cancellationToken:ct));
        await MovementAsync(db,tx,assetId,link.StoreItemId,damaged ? "Quarantine" : "Return",
            damaged ? "Damaged item received; excluded from available stock" : "Usable asset returned to Store Room",actor,ct);
    }

    public static async Task QuarantineAsync(DbConnection db, DbTransaction tx, long assetId, string actor, CancellationToken ct)
    {
        var link = await GetAsync(db,tx,assetId,ct);
        if (link is null || !link.PhysicallyInStore) return;
        // A damaged asset returned previously was already excluded from usable QuantityOnHand.
        if (link.Status.Equals("Available",StringComparison.OrdinalIgnoreCase))
        {
            if (link.QuantityOnHand-link.ReservedQuantity < 1)
                throw new InvalidOperationException("Insufficient available Store Room stock to quarantine the asset.");
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE StoreItems SET QuantityOnHand=QuantityOnHand-1,UpdatedAtUtc=@now WHERE Id=@id
                """,new {id=link.StoreItemId,now=DateTime.UtcNow},tx,cancellationToken:ct));
            await MovementAsync(db,tx,assetId,link.StoreItemId,"Quarantine","Moved from usable stock pending assessment/write-off",actor,ct);
        }
    }

    public static async Task RestoreAsync(DbConnection db, DbTransaction tx, long assetId, string actor, CancellationToken ct)
    {
        var link=await GetAsync(db,tx,assetId,ct);
        if(link is null || !link.PhysicallyInStore) return;
        if(link.Status.Equals("Available",StringComparison.OrdinalIgnoreCase)) return;
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems SET QuantityOnHand=QuantityOnHand+1,UpdatedAtUtc=@now WHERE Id=@id
            """,new {id=link.StoreItemId,now=DateTime.UtcNow},tx,cancellationToken:ct));
        await MovementAsync(db,tx,assetId,link.StoreItemId,"Return","Inspected / repaired and released to usable stock",actor,ct);
    }

    public static async Task DisposeAsync(DbConnection db, DbTransaction tx, long assetId, string actor, CancellationToken ct)
    {
        var link=await GetAsync(db,tx,assetId,ct);
        if(link is null) return;
        if(!link.PhysicallyInStore)
            throw new InvalidOperationException("Physical custody of the linked asset must be confirmed in Store Room before disposal.");
        if(link.Status.Equals("Available",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Asset must be quarantined before disposal.");
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreAssetLinks SET PhysicallyInStore=0 WHERE AssetId=@assetId
            """,new {assetId},tx,cancellationToken:ct));
        await MovementAsync(db,tx,assetId,link.StoreItemId,"Write-Off","Approved asset disposed; removed from physical custody",actor,ct);
    }
}
