namespace RoyalDesk.Api.HelpDesk;

public interface IHelpDeskRepository
{
    Task<IReadOnlyList<HelpDeskTicketDto>> GetTicketsAsync(string? status, CancellationToken ct = default);
    Task<HelpDeskTicketDto?> GetTicketAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketStatusHistoryDto>> GetHistoryAsync(int ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<ITTeamMemberDto>> GetTeamAsync(CancellationToken ct = default);
    Task<ITTeamMemberDto?> GetActiveTeamMemberByUsernameAsync(string username, CancellationToken ct = default);
    Task<ITTeamMemberDto> CreateTeamMemberAsync(CreateITTeamMemberDto dto, CancellationToken ct = default);
    Task AssignAsync(int ticketId, ITTeamMemberDto member, string actor, CancellationToken ct = default);
    Task StartAsync(int ticketId, ITTeamMemberDto member, string actor, CancellationToken ct = default);
    Task UpdateStatusAsync(int ticketId, UpdateTicketStatusDto dto, string actor, string? actorName, CancellationToken ct = default);
}
