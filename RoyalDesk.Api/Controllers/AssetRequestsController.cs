using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/asset-requests")]
[Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
public sealed class AssetRequestsController(
    IAssetRequestService service,
    CreateAssetRequestValidator validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AssetRequestResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequestDto request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var requestedBy = User.Identity!.Name!;
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Services;
using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/asset-requests")]
[Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
public sealed class AssetRequestsController(
    IAssetRequestService service,
    CreateAssetRequestValidator validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AssetRequestResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequestDto request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var requestedBy = User.Identity!.Name!;
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
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
    [ProducesResponseType<AssetRequestResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequestDto request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var requestedBy = User.Identity?.Name ?? "demo.user";
        var result = await service.CreateAsync(request, requestedBy, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
