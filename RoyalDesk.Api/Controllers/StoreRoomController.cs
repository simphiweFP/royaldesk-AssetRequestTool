using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/store-room")]
public sealed class StoreRoomController(StoreRoomService service) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? "unknown";

    [HttpGet("items")]
    public Task<IReadOnlyList<StoreItemDto>> Items(CancellationToken ct)
        => service.GetItemsAsync(ct);

    [HttpGet("movements")]
    public Task<IReadOnlyList<StoreMovementDto>> Movements(CancellationToken ct)
        => service.GetMovementsAsync(ct);

    [HttpGet("asset-inventory")]
    public Task<IReadOnlyList<StoreLinkedAssetDto>> AssetInventory(CancellationToken ct)
        => service.GetAssetInventoryAsync(ct);

    [HttpPost("items/{id:int}/link-asset")]
    public async Task<IActionResult> LinkAsset(int id, LinkStoreAssetDto dto, CancellationToken ct)
    {
        await service.LinkAssetAsync(id, dto, Actor, ct);
        return NoContent();
    }

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
