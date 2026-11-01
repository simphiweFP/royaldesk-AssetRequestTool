using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.HelpDesk;

public sealed class JustIctEscalationRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<int> CreateAsync(int ticketId, string providerEmail, EscalateToJustIctDto dto,
        string actor, string actorName, string fromStatus, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO TicketEscalations
                (TicketId, Provider, ProviderEmail, TeamViewerId, IssueDescription, Troubleshooting,
                 AdditionalNotes, EmailBody, EscalatedBy, EscalatedByName, EscalatedAtUtc)
            VALUES
                (@TicketId, 'Just ICT', @ProviderEmail, @TeamViewerId, @IssueDescription, @Troubleshooting,
                 @AdditionalNotes, @EmailBody, @EscalatedBy, @EscalatedByName, @EscalatedAtUtc);
            SELECT last_insert_rowid();
            """, new
        {
            TicketId = ticketId,
            ProviderEmail = providerEmail,
            TeamViewerId = dto.TeamViewerId.Trim(),
            IssueDescription = dto.IssueDescription.Trim(),
            Troubleshooting = dto.Troubleshooting.Trim(),
            AdditionalNotes = string.IsNullOrWhiteSpace(dto.AdditionalNotes) ? null : dto.AdditionalNotes.Trim(),
            EmailBody = dto.EmailBody.Trim(),
            EscalatedBy = actor,
            EscalatedByName = actorName,
            EscalatedAtUtc = now
        }, transaction, cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests SET Status='Escalated to Just ICT', LastUpdatedAtUtc=@Now WHERE Id=@TicketId;
            """, new { TicketId = ticketId, Now = now }, transaction, cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
            VALUES
                (@TicketId, @FromStatus, 'Escalated to Just ICT', @ChangedBy, @ChangedByName,
                 'Your request has been escalated to Just ICT for further assistance.', 1, @ChangedAtUtc);
            """, new { TicketId = ticketId, FromStatus = fromStatus, ChangedBy = actor,
                ChangedByName = actorName, ChangedAtUtc = now }, transaction, cancellationToken: ct));

        await transaction.CommitAsync(ct);
        return id;
    }
}
