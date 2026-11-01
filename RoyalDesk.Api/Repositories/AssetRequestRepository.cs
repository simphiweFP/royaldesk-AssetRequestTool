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
                (TicketNumber, RequesterName, RequesterEmail, Branch, Department, ItemType, Quantity, Reason, Status, RequestedBy, RequestedAtUtc)
            VALUES
                (NULL, @RequesterName, @RequesterEmail, @Branch, @Department, @ItemType, @Quantity, @Reason, @Status, @RequestedBy, @RequestedAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var id = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(insertSql, request, cancellationToken: cancellationToken));

        request.TicketNumber = $"RT-IT-{id:000000}";

        const string updateSql = """
            UPDATE AssetRequests
            SET TicketNumber = @TicketNumber
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new { Id = id, request.TicketNumber },
                cancellationToken: cancellationToken));

        return id;
    }
}
