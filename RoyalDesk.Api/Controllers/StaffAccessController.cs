using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.StaffAccess;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/staff-access")]
public sealed class StaffAccessController(StaffAccessService service) : ControllerBase
{
    [HttpGet("bootstrap-required")]
    public async Task<object> BootstrapRequired(CancellationToken ct)
        => new { required = await service.BootstrapRequiredAsync(ct) };

    [HttpPost("bootstrap")]
    public Task<StaffProfileDto> Bootstrap(BootstrapStaffDto dto, CancellationToken ct)
        => service.BootstrapAsync(dto, ct);

    [HttpGet("profiles/public")]
    public Task<IReadOnlyList<StaffProfileDto>> PublicProfiles(CancellationToken ct)
        => service.GetProfilesAsync(true, ct);

    [HttpPost("unlock")]
    public async Task<ActionResult<StaffSessionDto>> Unlock(UnlockStaffDto dto, CancellationToken ct)
    {
        var result = await service.UnlockAsync(dto, ct);
        Response.Cookies.Append(StaffAccessMiddleware.CookieName, result.RawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.Session.ExpiresAtUtc
        });
        return Ok(result.Session);
    }

    [HttpPost("lock")]
    public async Task<IActionResult> Lock(CancellationToken ct)
    {
        await service.LockAsync(Request.Cookies[StaffAccessMiddleware.CookieName], ct);
        Response.Cookies.Delete(StaffAccessMiddleware.CookieName);
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (HttpContext.Items["RoyalDeskStaffSession"] is not StaffSessionDto session) return Unauthorized();
        return Ok(session);
    }

    [HttpGet("profiles")]
    [RequirePage("access-management")]
    public Task<IReadOnlyList<StaffProfileDto>> Profiles(CancellationToken ct)
        => service.GetProfilesAsync(false, ct);

    [HttpPost("profiles")]
    [RequirePage("access-management")]
    public Task<StaffProfileDto> Create(CreateStaffAccessDto dto, CancellationToken ct)
        => service.CreateAsync(dto, User.Identity?.Name ?? "unknown", ct);

    [HttpPut("profiles/{id:int}")]
    [RequirePage("access-management")]
    public async Task<IActionResult> Update(int id, UpdateStaffAccessDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("profiles/{id:int}/reset-password")]
    [RequirePage("access-management")]
    public async Task<IActionResult> ResetPassword(int id, ResetStaffPasswordDto dto, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, dto.Password, ct);
        return NoContent();
    }
}
