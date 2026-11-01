using Microsoft.Extensions.Options;
using RoyalDesk.Api.Email;

namespace RoyalDesk.Api.HelpDesk;

public sealed class JustIctEscalationService(
    JustIctEscalationRepository escalationRepository,
    IHelpDeskRepository helpDeskRepository,
    IEmailOutbox outbox,
    TicketNotificationService notifications,
    IOptions<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value;

    public async Task<TicketEscalationDto> EscalateAsync(int ticketId, EscalateToJustIctDto dto, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.TeamViewerId)) throw new ArgumentException("TeamViewer ID is required.");
        if (string.IsNullOrWhiteSpace(dto.IssueDescription)) throw new ArgumentException("Issue description is required.");
        if (string.IsNullOrWhiteSpace(dto.Troubleshooting)) throw new ArgumentException("Troubleshooting completed is required.");
        if (string.IsNullOrWhiteSpace(dto.EmailBody)) throw new ArgumentException("Escalation email body is required.");

        var member = await helpDeskRepository.GetActiveTeamMemberByUsernameAsync(actor, ct)
            ?? throw new InvalidOperationException("The signed-in user is not configured as an active IT Team member.");
        var ticket = await helpDeskRepository.GetTicketAsync(ticketId, ct)
            ?? throw new KeyNotFoundException("Ticket not found.");

        if (ticket.AssignedToId != member.Id)
            throw new InvalidOperationException("Only the assigned technician can escalate this ticket.");
        if (ticket.Status is "Resolved" or "Closed" or "Cancelled")
            throw new InvalidOperationException("Completed tickets cannot be escalated.");

        var id = await escalationRepository.CreateAsync(ticketId, _options.JustIctAddress, dto,
            actor, member.DisplayName, ticket.Status, ct);

        await outbox.EnqueueAndTrySendAsync(ticketId, _options.JustIctAddress, _options.ItTeamAddress,
            $"RoyalDesk {ticket.TicketNumber} - Just ICT Escalation", dto.EmailBody.Trim(), ct);

        var updated = await helpDeskRepository.GetTicketAsync(ticketId, ct);
        if (updated is not null)
            await notifications.StatusChangedAsync(updated, "Escalated to Just ICT", "Just ICT has been engaged for further assistance.", ct);

        return new TicketEscalationDto
        {
            Id = id,
            TicketId = ticketId,
            Provider = "Just ICT",
            ProviderEmail = _options.JustIctAddress,
            TeamViewerId = dto.TeamViewerId.Trim(),
            IssueDescription = dto.IssueDescription.Trim(),
            Troubleshooting = dto.Troubleshooting.Trim(),
            AdditionalNotes = string.IsNullOrWhiteSpace(dto.AdditionalNotes) ? null : dto.AdditionalNotes.Trim(),
            EscalatedByName = member.DisplayName,
            EscalatedAtUtc = DateTime.UtcNow
        };
    }
}
