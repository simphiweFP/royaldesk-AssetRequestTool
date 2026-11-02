using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/store-room")]
[Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
public sealed class StoreRoomController(StoreRoomService service) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? "unknown";

    [HttpGet("items")]
    public Task<IReadOnlyList<StoreItemDto>> Items(CancellationToken ct)
        => service.GetItemsAsync(ct);

    [HttpGet("movements")]
    public Task<IReadOnlyList<StoreMovementDto>> Movements(CancellationToken ct)
        => service.GetMovementsAsync(ct);

    [HttpPost("items")]
    public async Task<ActionResult<StoreItemDto>> CreateItem(CreateStoreItemDto dto, CancellationToken ct)
        => Ok(await service.CreateItemAsync(dto, Actor, ct));

    [HttpPost("items/{id:int}/movements")]
    public async Task<IActionResult> Move(int id, StoreMovementRequest request, CancellationToken ct)
    {
        await service.ApplyMovementAsync(id, request, Actor, ct);
        return NoContent();
    }
}
