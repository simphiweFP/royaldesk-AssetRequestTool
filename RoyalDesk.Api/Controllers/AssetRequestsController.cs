using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/asset-requests")]
public sealed class AssetRequestsController(
    IAssetRequestService service,
    CreateAssetRequestValidator validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequestDto request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        var requestedBy = string.IsNullOrWhiteSpace(request.RequesterName) ? "RoyalDesk employee" : request.RequesterName.Trim();
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
