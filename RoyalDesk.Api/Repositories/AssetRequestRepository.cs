using Dapper;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Models;

namespace RoyalDesk.Api.Repositories;

public sealed class AssetRequestRepository(IDbConnectionFactory connectionFactory) : IAssetRequestRepository
{
    public async Task<int> CreateAsync(AssetRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AssetRequests
                (Branch, Department, ItemType, Quantity, Reason, RequestedBy, RequestedAtUtc)
            VALUES
                (@Branch, @Department, @ItemType, @Quantity, @Reason, @RequestedBy, @RequestedAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));
    }
}
