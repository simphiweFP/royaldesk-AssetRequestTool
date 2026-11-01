using System.Security.Cryptography;
using System.Text;
using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.HelpDesk;

public class PublicTicketSummaryDto
{
    public string TicketNumber { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
    public string? LatestPublicUpdate { get; set; }
}

public sealed class PublicTicketDetailDto : PublicTicketSummaryDto
{
    public string Branch { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public IReadOnlyList<PublicTicketHistoryDto> History { get; set; } = [];
}

public sealed class PublicTicketHistoryDto
{
    public string Status { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public string? Update { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}

public sealed class VerifyTicketRequest
{
    public string TicketNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? TrackingPin { get; set; }
}

internal sealed class TrackingTicketRow
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string? TrackingPinHash { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
}

public static class TrackingPinSecurity
{
    public static string GeneratePin() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public static string Hash(string pin)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin.Trim())));

    public static bool Verify(string pin, string expectedHash)
    {
        var actual = Convert.FromHexString(Hash(pin));
        var expected = Convert.FromHexString(expectedHash);
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed class PublicTrackingService(IDbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<PublicTicketSummaryDto>> SearchAsync(string name, string department, CancellationToken ct = default)
    {
        name = name?.Trim() ?? string.Empty;
        department = department?.Trim() ?? string.Empty;
        if (name.Length < 2 || department.Length < 2)
            throw new ArgumentException("Name and department are required.");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<PublicTicketSummaryDto>(new CommandDefinition("""
            SELECT r.TicketNumber, r.RequesterName, r.Department, r.ItemType, r.Status,
                   r.AssignedToName, r.RequestedAtUtc, r.LastUpdatedAtUtc,
                   (SELECT h.Comment
                    FROM TicketStatusHistory h
                    WHERE h.TicketId = r.Id AND h.PublicVisible = 1 AND h.Comment IS NOT NULL AND h.Comment <> ''
                    ORDER BY h.ChangedAtUtc DESC, h.Id DESC LIMIT 1) AS LatestPublicUpdate
            FROM AssetRequests r
            WHERE lower(r.RequesterName) LIKE '%' || lower(@Name) || '%'
              AND lower(r.Department) = lower(@Department)
            ORDER BY r.RequestedAtUtc DESC
            LIMIT 20;
            """, new { Name = name, Department = department }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<PublicTicketDetailDto?> VerifyAndGetAsync(VerifyTicketRequest request, CancellationToken ct = default)
    {
        var ticketNumber = request.TicketNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ticketNumber)) throw new ArgumentException("Ticket number is required.");

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<TrackingTicketRow>(new CommandDefinition("""
            SELECT Id, TicketNumber, RequesterName, RequesterEmail, TrackingPinHash, Branch, Department,
                   ItemType, Quantity, Reason, Status, AssignedToName, RequestedAtUtc, AssignedAtUtc,
                   StartedAtUtc, ResolvedAtUtc, LastUpdatedAtUtc
            FROM AssetRequests
            WHERE lower(TicketNumber) = lower(@TicketNumber)
            LIMIT 1;
            """, new { TicketNumber = ticketNumber }, cancellationToken: ct));
        if (row is null) return null;

        var verified = !string.IsNullOrWhiteSpace(row.RequesterEmail)
            ? !string.IsNullOrWhiteSpace(request.Email) && string.Equals(row.RequesterEmail.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase)
            : !string.IsNullOrWhiteSpace(row.TrackingPinHash) && !string.IsNullOrWhiteSpace(request.TrackingPin) && TrackingPinSecurity.Verify(request.TrackingPin, row.TrackingPinHash);

        if (!verified) return null;

        var history = (await connection.QueryAsync<PublicTicketHistoryDto>(new CommandDefinition("""
            SELECT ToStatus AS Status, ChangedByName AS TechnicianName, Comment AS [Update], ChangedAtUtc
            FROM TicketStatusHistory
            WHERE TicketId = @TicketId AND PublicVisible = 1
            ORDER BY ChangedAtUtc, Id;
            """, new { TicketId = row.Id }, cancellationToken: ct))).AsList();

        return new PublicTicketDetailDto
        {
            TicketNumber = row.TicketNumber,
            RequesterName = row.RequesterName,
            Department = row.Department,
            ItemType = row.ItemType,
            Status = row.Status,
            AssignedToName = row.AssignedToName,
            RequestedAtUtc = row.RequestedAtUtc,
            LastUpdatedAtUtc = row.LastUpdatedAtUtc,
            Branch = row.Branch,
            Quantity = row.Quantity,
            Reason = row.Reason,
            AssignedAtUtc = row.AssignedAtUtc,
            StartedAtUtc = row.StartedAtUtc,
            ResolvedAtUtc = row.ResolvedAtUtc,
            History = history
        };
    }
}
