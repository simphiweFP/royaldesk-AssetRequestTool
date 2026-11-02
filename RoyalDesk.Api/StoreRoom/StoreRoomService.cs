using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.StoreRoom;

public sealed class StoreRoomService(IDbConnectionFactory connectionFactory)
{
    private static readonly HashSet<string> AllowedMovements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Stock In", "Reserve", "Release Reservation", "Issue", "Return", "Adjustment", "Write-Off"
    };

    public async Task<IReadOnlyList<StoreItemDto>> GetItemsAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<StoreItemDto>(new CommandDefinition("""
            SELECT Id, Name, Category, Sku, Location, QuantityOnHand, ReservedQuantity,
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

        var onHand = item.QuantityOnHand;
        var reserved = item.ReservedQuantity;
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
        }

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE StoreItems
            SET QuantityOnHand = @OnHand, ReservedQuantity = @Reserved, UpdatedAtUtc = @Now
            WHERE Id = @Id;
            """, new { Id = itemId, OnHand = onHand, Reserved = reserved, Now = now }, tx, cancellationToken: ct));

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
