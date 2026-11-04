using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/asset-requests")]
public sealed class AssetRequestsController(
    IAssetRequestService service,
    CreateAssetRequestValidator validator,
    RequestAttachmentService attachments) : ControllerBase
{
    [HttpPost("with-evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> CreateWithEvidence([FromForm] string requestJson, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        CreateAssetRequestDto? request;
        try { request = JsonSerializer.Deserialize<CreateAssetRequestDto>(requestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return BadRequest(new { detail = "Invalid request details." }); }
        if (request is null) return BadRequest(new { detail = "Request details are required." });
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var stockCode = request.RequestType.Equals("Service", StringComparison.OrdinalIgnoreCase)
            && request.ItemType.Equals("Stock Code Creation", StringComparison.OrdinalIgnoreCase);
        var incident = request.RequestType.Equals("Incident", StringComparison.OrdinalIgnoreCase);
        List<ValidatedAttachment> evidence;
        try { evidence = await attachments.ValidateAsync(files, stockCode, incident, cancellationToken); }
        catch (ArgumentException ex) { return BadRequest(new { detail = ex.Message }); }
        var requestedBy = string.IsNullOrWhiteSpace(request.RequesterName) ? "RoyalDesk employee" : request.RequesterName.Trim();
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);
        await attachments.SaveAsync(result.Id, evidence, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequestDto request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        if (request.RequestType.Equals("Service", StringComparison.OrdinalIgnoreCase)
            && request.ItemType.Equals("Stock Code Creation", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { detail = "Stock code creation requires a supporting document. Use the evidence upload form." });

        var requestedBy = string.IsNullOrWhiteSpace(request.RequesterName) ? "RoyalDesk employee" : request.RequesterName.Trim();
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
