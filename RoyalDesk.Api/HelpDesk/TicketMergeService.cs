using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.HelpDesk;

public sealed class MergeTicketsRequest
{
    public int PrimaryTicketId { get; set; }
    public List<int> SecondaryTicketIds { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
    public bool ConfirmDifferentRequesters { get; set; }
}

public sealed record TicketMergeRecord(int PrimaryTicketId, int MergedTicketId,
    string MergedBy, string Reason, DateTime MergedAtUtc);

/// <summary>
/// Additive, reversible linkage. Original tickets and their history/attachments are never deleted.
/// Only authorised ticket staff may invoke this service via the staff-protected controller.
/// </summary>
public sealed class TicketMergeService(IDbConnectionFactory connections, IHelpDeskRepository staff)
{
    public async Task<IReadOnlyList<TicketMergeRecord>> GetMergedAsync(int primaryTicketId, CancellationToken ct)
    {
        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);
        var records = await db.QueryAsync<TicketMergeRecord>(new CommandDefinition(
            "SELECT PrimaryTicketId, MergedTicketId, MergedBy, Reason, MergedAtUtc FROM TicketMerges WHERE PrimaryTicketId = @primaryTicketId ORDER BY Id",
            new { primaryTicketId }, cancellationToken: ct));
        return records.AsList();
    }

    public async Task<IReadOnlyList<TicketMergeRecord>> MergeAsync(MergeTicketsRequest request, string actor, CancellationToken ct)
    {
        var member = await staff.GetActiveTeamMemberByUsernameAsync(actor, ct)
            ?? throw new UnauthorizedAccessException("Only active IT team members may merge tickets.");
        if (member.Role is not ("IT Administrator" or "IT Technician" or "IT Admin"))
            throw new UnauthorizedAccessException("An IT administrator or technician role is required.");
        if (request.PrimaryTicketId <= 0 || request.SecondaryTicketIds is null ||
            request.SecondaryTicketIds.Count == 0 || request.SecondaryTicketIds.Count > 20 ||
            request.SecondaryTicketIds.Any(id => id <= 0 || id == request.PrimaryTicketId) ||
            request.SecondaryTicketIds.Distinct().Count() != request.SecondaryTicketIds.Count)
            throw new ArgumentException("Choose one primary ticket and 1–20 distinct secondary tickets.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            throw new ArgumentException("A merge reason between 1 and 1000 characters is required.");

        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var ids = new[] { request.PrimaryTicketId }.Concat(request.SecondaryTicketIds).ToArray();
        var tickets = (await db.QueryAsync<HelpDeskTicketDto>(new CommandDefinition(
            "SELECT Id, TicketNumber, RequesterEmail, RequesterName, RequestType, Status FROM AssetRequests WHERE Id IN @ids",
            new { ids }, tx, cancellationToken: ct))).ToDictionary(t => t.Id);
        if (tickets.Count != ids.Length) throw new KeyNotFoundException("One or more tickets do not exist.");
        if (tickets.Values.Any(t => t.Status == "Merged"))
            throw new InvalidOperationException("Tickets already merged into another ticket cannot be merged again.");

        var existingLinks = await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM TicketMerges WHERE MergedTicketId IN @ids OR PrimaryTicketId IN @secondary",
            new { ids, secondary = request.SecondaryTicketIds }, tx, cancellationToken: ct));
        if (existingLinks > 0)
            throw new InvalidOperationException("A selected ticket is already merged or owns other merged tickets.");

        // Delivery fulfilment is intentionally not mergeable: it has independent asset/receipt controls.
        var activeDeliveries = await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM EquipmentDeliveries WHERE TicketId IN @ids",
            new { ids }, tx, cancellationToken: ct));
        if (activeDeliveries > 0)
            throw new InvalidOperationException("Tickets linked to equipment deliveries cannot be merged.");

        // Different requesters require an explicit confirmation; no automatic visibility is granted.
        var primary = tickets[request.PrimaryTicketId];
        if (!request.ConfirmDifferentRequesters && request.SecondaryTicketIds.Any(id =>
            !string.Equals(tickets[id].RequesterEmail?.Trim(), primary.RequesterEmail?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(tickets[id].RequesterName?.Trim(), primary.RequesterName?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Different requesters detected. Explicit confirmation is required.");

        var now = DateTime.UtcNow;
        var results = new List<TicketMergeRecord>();
        foreach (var childId in request.SecondaryTicketIds)
        {
            var child = tickets[childId];
            await db.ExecuteAsync(new CommandDefinition(
                "INSERT INTO TicketMerges (PrimaryTicketId, MergedTicketId, MergedBy, Reason, MergedAtUtc) VALUES (@primary, @childId, @actor, @reason, @now)",
                new { primary = primary.Id, childId, actor, reason = request.Reason.Trim(), now }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition(
                "UPDATE AssetRequests SET Status = 'Merged', LastUpdatedAtUtc = @now WHERE Id = @childId",
                new { now, childId }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
                VALUES (@childId, @previous, 'Merged', @actor, @name, @comment, 0, @now)
                """, new { childId, previous = child.Status, actor, name = member.DisplayName,
                    comment = $"Merged into {primary.TicketNumber}. {request.Reason.Trim()}", now }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
                VALUES (@primary, @status, @status, @actor, @name, @comment, 0, @now)
                """, new { primary = primary.Id, status = primary.Status, actor, name = member.DisplayName,
                    comment = $"Ticket {child.TicketNumber} merged into this ticket. {request.Reason.Trim()}", now }, tx, cancellationToken: ct));
            results.Add(new(primary.Id, childId, actor, request.Reason.Trim(), now));
        }

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE AssetRequests SET LastUpdatedAtUtc = @now WHERE Id = @id",
            new { now, id = primary.Id }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return results;
    }
}
