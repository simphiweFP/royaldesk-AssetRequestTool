using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.HelpDesk;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/public/tickets")]
[AllowAnonymous]
public sealed class PublicTrackingController(PublicTrackingService service) : ControllerBase
{
    [HttpGet("search")]
    public Task<IReadOnlyList<PublicTicketSummaryDto>> Search(
        [FromQuery] string name,
        [FromQuery] string department,
        CancellationToken ct)
        => service.SearchAsync(name, department, ct);

    [HttpPost("verify")]
    public async Task<ActionResult<PublicTicketDetailDto>> Verify(VerifyTicketRequest request, CancellationToken ct)
    {
        var ticket = await service.VerifyAndGetAsync(request, ct);
        return ticket is null ? NotFound("Ticket not found or verification details do not match.") : Ok(ticket);
    }
}
