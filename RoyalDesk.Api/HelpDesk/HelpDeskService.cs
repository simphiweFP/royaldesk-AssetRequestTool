using RoyalDesk.Api.Email;

namespace RoyalDesk.Api.HelpDesk;

public sealed class HelpDeskService(IHelpDeskRepository repository, TicketNotificationService notifications)
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Assigned", "In Progress", "Waiting for User", "Waiting for Third Party",
        "Escalated to Just ICT", "Resolved", "Closed", "Cancelled"
    };

    public Task<IReadOnlyList<HelpDeskTicketDto>> GetTicketsAsync(string? status, CancellationToken ct = default)
        => repository.GetTicketsAsync(status, ct);

    public Task<HelpDeskTicketDto?> GetTicketAsync(int id, CancellationToken ct = default)
        => repository.GetTicketAsync(id, ct);

    public Task<IReadOnlyList<TicketStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct = default)
        => repository.GetHistoryAsync(id, ct);

    public Task<IReadOnlyList<ITTeamMemberDto>> GetTeamAsync(CancellationToken ct = default)
        => repository.GetTeamAsync(ct);

    public Task<ITTeamMemberDto> CreateTeamMemberAsync(CreateITTeamMemberDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) || string.IsNullOrWhiteSpace(dto.Username))
            throw new ArgumentException("Display name and username are required.");
        return repository.CreateTeamMemberAsync(dto, ct);
    }

    public async Task AssignToMeAsync(int id, string actor, CancellationToken ct = default)
    {
        var member = await RequireTeamMemberAsync(actor, ct);
        await repository.AssignAsync(id, member, actor, ct);
        var ticket = await repository.GetTicketAsync(id, ct);
        if (ticket is not null)
            await notifications.AssignedAsync(ticket, member.DisplayName, ct);
    }

    public async Task StartAsync(int id, string actor, CancellationToken ct = default)
    {
        var member = await RequireTeamMemberAsync(actor, ct);
        await repository.StartAsync(id, member, actor, ct);
        var ticket = await repository.GetTicketAsync(id, ct);
        if (ticket is not null)
            await notifications.StartedAsync(ticket, member.DisplayName, ct);
    }

    public async Task UpdateStatusAsync(int id, UpdateTicketStatusDto dto, string actor, CancellationToken ct = default)
    {
        if (!AllowedStatuses.Contains(dto.Status)) throw new ArgumentException("Unsupported ticket status.");
        var member = await RequireTeamMemberAsync(actor, ct);
        await repository.UpdateStatusAsync(id, dto, actor, member.DisplayName, ct);
        var ticket = await repository.GetTicketAsync(id, ct);
        if (ticket is not null && dto.PublicVisible)
            await notifications.StatusChangedAsync(ticket, dto.Status, dto.Comment, ct);
    }

    private async Task<ITTeamMemberDto> RequireTeamMemberAsync(string actor, CancellationToken ct)
        => await repository.GetActiveTeamMemberByUsernameAsync(actor, ct)
           ?? throw new InvalidOperationException("The signed-in user is not configured as an active IT Team member.");
}
