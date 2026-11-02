using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.BusinessValue;

public sealed class BusinessValueService(IDbConnectionFactory connectionFactory)
{
    public async Task<BusinessValueSummaryDto> GetAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        var summary = new BusinessValueSummaryDto
        {
            TotalTickets = await ScalarInt(connection, "SELECT COUNT(*) FROM AssetRequests;", ct),
            OpenTickets = await ScalarInt(connection, "SELECT COUNT(*) FROM AssetRequests WHERE Status NOT IN ('Resolved','Closed','Cancelled');", ct),
            ResolvedTickets = await ScalarInt(connection, "SELECT COUNT(*) FROM AssetRequests WHERE Status IN ('Resolved','Closed');", ct),
            EscalatedTickets = await ScalarInt(connection, "SELECT COUNT(*) FROM AssetRequests WHERE Status IN ('Escalated to Just ICT','Waiting for Third Party') OR Id IN (SELECT TicketId FROM TicketEscalations);", ct),
            AverageResponseMinutes = await ScalarDouble(connection, "SELECT COALESCE(AVG((julianday(AssignedAtUtc)-julianday(RequestedAtUtc))*1440.0),0) FROM AssetRequests WHERE AssignedAtUtc IS NOT NULL;", ct),
            AverageResolutionMinutes = await ScalarDouble(connection, "SELECT COALESCE(AVG((julianday(ResolvedAtUtc)-julianday(RequestedAtUtc))*1440.0),0) FROM AssetRequests WHERE ResolvedAtUtc IS NOT NULL;", ct),
            TotalStoreUnits = await ScalarInt(connection, "SELECT COALESCE(SUM(QuantityOnHand),0) FROM StoreItems WHERE IsActive=1;", ct),
            ReservedStoreUnits = await ScalarInt(connection, "SELECT COALESCE(SUM(ReservedQuantity),0) FROM StoreItems WHERE IsActive=1;", ct),
            LowStockItems = await ScalarInt(connection, "SELECT COUNT(*) FROM StoreItems WHERE IsActive=1 AND (QuantityOnHand-ReservedQuantity)<=MinimumStock;", ct),
            StoreUnitsIssued = await ScalarInt(connection, "SELECT COALESCE(SUM(Quantity),0) FROM StoreMovements WHERE MovementType='Issue';", ct),
            TotalAssets = await ScalarInt(connection, "SELECT COUNT(*) FROM Assets;", ct),
            AvailableAssets = await ScalarInt(connection, "SELECT COUNT(*) FROM Assets WHERE Status='Available';", ct),
            AvailableAssetValue = await ScalarDecimal(connection, "SELECT COALESCE(SUM(CASE WHEN Status='Available' THEN COALESCE(PurchaseValue,0) ELSE 0 END),0) FROM Assets;", ct),
            EstimatedReuseValue = await ScalarDecimal(connection, "SELECT COALESCE(SUM(CASE WHEN Status='Available' AND COALESCE(FullyDepreciated,0)=0 THEN COALESCE(PurchaseValue,0) ELSE 0 END),0) FROM Assets;", ct)
        };

        summary.TicketsByStatus = (await connection.QueryAsync<BusinessValueBreakdownDto>(new CommandDefinition("""
            SELECT Status AS Name, COUNT(*) AS Count
            FROM AssetRequests
            GROUP BY Status
            ORDER BY Count DESC, Name;
            """, cancellationToken: ct))).AsList();

        summary.TicketsByCategory = (await connection.QueryAsync<BusinessValueBreakdownDto>(new CommandDefinition("""
            SELECT ItemType AS Name, COUNT(*) AS Count
            FROM AssetRequests
            GROUP BY ItemType
            ORDER BY Count DESC, Name
            LIMIT 10;
            """, cancellationToken: ct))).AsList();

        summary.TechnicianWorkload = (await connection.QueryAsync<TechnicianWorkloadDto>(new CommandDefinition("""
            SELECT
                COALESCE(t.DisplayName, r.AssignedToName, 'Unassigned') AS TechnicianName,
                COUNT(r.Id) AS Assigned,
                SUM(CASE WHEN r.Status IN ('Assigned','In Progress','Waiting for User','Waiting for Third Party','Escalated to Just ICT') THEN 1 ELSE 0 END) AS Active,
                SUM(CASE WHEN r.Status IN ('Resolved','Closed') THEN 1 ELSE 0 END) AS Resolved
            FROM AssetRequests r
            LEFT JOIN ITTeamMembers t ON t.Id = r.AssignedToId
            WHERE r.AssignedToId IS NOT NULL OR r.AssignedToName IS NOT NULL
            GROUP BY COALESCE(t.DisplayName, r.AssignedToName, 'Unassigned')
            ORDER BY Active DESC, Assigned DESC, TechnicianName;
            """, cancellationToken: ct))).AsList();

        summary.StoreMovementsByType = (await connection.QueryAsync<BusinessValueBreakdownDto>(new CommandDefinition("""
            SELECT MovementType AS Name, COALESCE(SUM(Quantity),0) AS Count
            FROM StoreMovements
            GROUP BY MovementType
            ORDER BY Count DESC, Name;
            """, cancellationToken: ct))).AsList();

        return summary;
    }

    private static Task<int> ScalarInt(System.Data.Common.DbConnection connection, string sql, CancellationToken ct)
        => connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));

    private static Task<double> ScalarDouble(System.Data.Common.DbConnection connection, string sql, CancellationToken ct)
        => connection.ExecuteScalarAsync<double>(new CommandDefinition(sql, cancellationToken: ct));

    private static Task<decimal> ScalarDecimal(System.Data.Common.DbConnection connection, string sql, CancellationToken ct)
        => connection.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, cancellationToken: ct));
}
