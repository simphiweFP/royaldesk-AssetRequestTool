using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.HelpDesk;
using RoyalDesk.Api.Deliveries;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.StaffAccess;
using RoyalDesk.Api.StoreRoom;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/helpdesk")]
public sealed class HelpDeskController(HelpDeskService service, JustIctEscalationService escalationService, RequestAttachmentService attachments, EquipmentDeliveryService deliveries, StoreRoomService storeRoom) : ControllerBase
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

    [HttpGet("tickets/{id:int}/attachments")]
    [RequirePage("tickets")]
    public Task<IReadOnlyList<AttachmentInfo>> Attachments(int id, CancellationToken ct)
        => attachments.ListAsync(id, ct);

    [HttpGet("tickets/{id:int}/attachments/{attachmentId:int}")]
    [RequirePage("tickets")]
    public async Task<IActionResult> DownloadAttachment(int id, int attachmentId, CancellationToken ct)
    {
        var attachment = await attachments.ReadAsync(id, attachmentId, ct);
        if (attachment is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(attachment.Content, "application/octet-stream", attachment.FileName);
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

    // Read-only fulfilment choices for authorised IT ticket staff.
    [HttpGet("delivery-stock")]
    public Task<IReadOnlyList<StoreItemDto>> DeliveryStock(CancellationToken ct)
        => storeRoom.GetItemsAsync(ct);

    [HttpGet("delivery-assets/{storeItemId:int}")]
    public async Task<IReadOnlyList<StoreLinkedAssetDto>> DeliveryAssets(int storeItemId,CancellationToken ct)
        => (await storeRoom.GetAssetInventoryAsync(ct))
            .Where(a=>a.StoreItemId==storeItemId && a.PhysicallyInStore &&
                a.Status.Equals("Available",StringComparison.OrdinalIgnoreCase) &&
                !a.Condition.Equals("Damaged",StringComparison.OrdinalIgnoreCase) &&
                !a.Condition.Equals("Obsolete",StringComparison.OrdinalIgnoreCase))
            .ToList();

    [HttpGet("tickets/{id:int}/delivery")]
    public async Task<IActionResult> Delivery(int id,CancellationToken ct)
    {
        var delivery=await deliveries.GetAsync(id,ct);
        return delivery is null?NotFound():Ok(delivery);
    }

    [HttpGet("deliveries/open")]
    public Task<IReadOnlyList<EquipmentDeliveryDto>> OpenDeliveries(CancellationToken ct)
        => deliveries.GetOpenAsync(ct);

    [HttpGet("tickets/{id:int}/delivery/history")]
    public Task<IReadOnlyList<EquipmentDeliveryEventDto>> DeliveryHistory(int id,CancellationToken ct)
        => deliveries.HistoryAsync(id,ct);

    [HttpPost("tickets/{id:int}/delivery/prepare")]
    public Task<EquipmentDeliveryDto> PrepareDelivery(int id,PrepareEquipmentDeliveryDto dto,CancellationToken ct)
        => deliveries.PrepareAsync(id,dto,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/cancel")]
    public Task<EquipmentDeliveryDto> CancelPreparedDelivery(int id,DeliveryExceptionDto dto,CancellationToken ct)
        => deliveries.CancelAsync(id,dto,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/retry")]
    public Task<EquipmentDeliveryDto> RetryDelivery(int id,CancellationToken ct)
        => deliveries.RetryAsync(id,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/pickup")]
    public Task<EquipmentDeliveryDto> PickupDelivery(int id,PickupEquipmentDeliveryDto dto,CancellationToken ct)
        => deliveries.PickupAsync(id,dto,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/arrived")]
    public Task<EquipmentDeliveryDto> MarkDeliveryArrived(int id,CancellationToken ct)
        => deliveries.ArrivedAsync(id,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/proof")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> UploadDeliveryProof(int id, [FromForm] IFormFile file,CancellationToken ct)
    {
        var delivery=await deliveries.GetAsync(id,ct);
        if(delivery is null)return NotFound();
        if(delivery.Status is "Waiting for Driver Pickup" or "Awaiting Local Handover")
            return Conflict("Upload the delivery proof after dispatch or during receipt confirmation.");
        if(file is null)return BadRequest("Attach a PDF or delivery photo.");
        var current=await attachments.ListAsync(id,ct);
        if(current.Count>=5)return BadRequest("This ticket already contains five attachments.");
        var validated=await attachments.ValidateAsync(new List<IFormFile>{file},false,true,ct);
        await attachments.SaveAsync(id,validated,ct);
        return NoContent();
    }

    [HttpPost("tickets/{id:int}/delivery/confirm")]
    public Task<EquipmentDeliveryDto> ConfirmDelivery(int id,ConfirmEquipmentReceiptDto dto,CancellationToken ct)
        => deliveries.ConfirmAsync(id,dto,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/fail")]
    public Task<EquipmentDeliveryDto> FailDelivery(int id,DeliveryExceptionDto dto,CancellationToken ct)
        => deliveries.FailAsync(id,dto,Actor,ct);

    [HttpPost("tickets/{id:int}/delivery/return")]
    public Task<EquipmentDeliveryDto> ReturnFailedDelivery(int id,DeliveryExceptionDto dto,CancellationToken ct)
        => deliveries.ReturnedAsync(id,dto,Actor,ct);

    [HttpGet("team")]
    public Task<IReadOnlyList<ITTeamMemberDto>> Team(CancellationToken ct)
        => service.GetTeamAsync(ct);

    [HttpPost("team")]
    public async Task<ActionResult<ITTeamMemberDto>> CreateTeamMember(CreateITTeamMemberDto dto, CancellationToken ct)
        => Ok(await service.CreateTeamMemberAsync(dto, ct));
}
