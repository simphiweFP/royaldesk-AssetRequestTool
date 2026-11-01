using Dapper;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Models;

namespace RoyalDesk.Api.Repositories;

public sealed class AssetRequestRepository(IDbConnectionFactory connectionFactory) : IAssetRequestRepository
{
    public async Task<int> CreateAsync(AssetRequest request, CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            INSERT INTO AssetRequests
                (TicketNumber, RequesterName, RequesterEmail, TrackingPinHash, Branch, Department, ItemType, Quantity, Reason, Status, RequestedBy, RequestedAtUtc)
            VALUES
                (NULL, @RequesterName, @RequesterEmail, @TrackingPinHash, @Branch, @Department, @ItemType, @Quantity, @Reason, @Status, @RequestedBy, @RequestedAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var id = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(insertSql, request, transaction, cancellationToken: cancellationToken));

        request.TicketNumber = $"RT-IT-{id:000000}";

        const string updateSql = """
            UPDATE AssetRequests
            SET TicketNumber = @TicketNumber,
                LastUpdatedAtUtc = @RequestedAtUtc
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            updateSql,
            new { Id = id, request.TicketNumber, request.RequestedAtUtc },
            transaction,
            cancellationToken: cancellationToken));

        const string historySql = """
            INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
            VALUES
                (@TicketId, NULL, 'New', @ChangedBy, @ChangedByName, 'Ticket created', 1, @ChangedAtUtc);
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            historySql,
            new
            {
                TicketId = id,
                ChangedBy = request.RequestedBy,
                ChangedByName = request.RequesterName,
                ChangedAtUtc = request.RequestedAtUtc
            },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return id;
    }
}
