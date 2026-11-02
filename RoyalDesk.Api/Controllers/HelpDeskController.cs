using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.HelpDesk;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/helpdesk")]
public sealed class HelpDeskController(HelpDeskService service, JustIctEscalationService escalationService) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? "unknown";

    [HttpGet("tickets")]
    public Task<IReadOnlyList<HelpDeskTicketDto>> Tickets([FromQuery] string? status, CancellationToken ct)
        => service.GetTicketsAsync(status, ct);

    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult<HelpDeskTicketDto>> Ticket(int id, CancellationToken ct)
    {
        var ticket = await service.GetTicketAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpGet("tickets/{id:int}/history")]
    public Task<IReadOnlyList<TicketStatusHistoryDto>> History(int id, CancellationToken ct)
        => service.GetHistoryAsync(id, ct);

    [HttpPost("tickets/{id:int}/assign-to-me")]
    public async Task<IActionResult> AssignToMe(int id, CancellationToken ct)
    {
        await service.AssignToMeAsync(id, Actor, ct);
        return NoContent();
    }

    [HttpPost("tickets/{id:int}/start")]
    public async Task<IActionResult> Start(int id, CancellationToken ct)
    {
        await service.StartAsync(id, Actor, ct);
        return NoContent();
    }

    [HttpPost("tickets/{id:int}/escalate-just-ict")]
    public async Task<ActionResult<TicketEscalationDto>> EscalateToJustIct(int id, EscalateToJustIctDto dto, CancellationToken ct)
        => Ok(await escalationService.EscalateAsync(id, dto, Actor, ct));

    [HttpPatch("tickets/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusDto dto, CancellationToken ct)
    {
        await service.UpdateStatusAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpGet("team")]
    public Task<IReadOnlyList<ITTeamMemberDto>> Team(CancellationToken ct)
        => service.GetTeamAsync(ct);

    [HttpPost("team")]
    public async Task<ActionResult<ITTeamMemberDto>> CreateTeamMember(CreateITTeamMemberDto dto, CancellationToken ct)
        => Ok(await service.CreateTeamMemberAsync(dto, ct));
}
