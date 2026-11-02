using System.Security.Claims;

namespace RoyalDesk.Api.StaffAccess;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePageAttribute(string pageKey) : Attribute
{
    public string PageKey { get; } = pageKey;
}

public sealed class StaffAccessMiddleware(RequestDelegate next)
{
    public const string CookieName = "RoyalDeskStaff";

    public async Task InvokeAsync(HttpContext context, StaffAccessService accessService)
    {
        var session = await accessService.ResolveSessionAsync(context.Request.Cookies[CookieName], context.RequestAborted);
        if (session is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.StaffAccessId.ToString()),
                new(ClaimTypes.Name, session.DisplayName)
            };
            if (!string.IsNullOrWhiteSpace(session.Username)) claims.Add(new("username", session.Username));
            claims.AddRange(session.Pages.Select(p => new Claim("page", p)));
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "RoyalDeskStaff"));
            context.Items["RoyalDeskStaffSession"] = session;
        }

        var required = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RequirePageAttribute>() ?? [];
        if (required.Count > 0)
        {
            if (session is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { detail = "RoyalDesk staff access is required." }, context.RequestAborted);
                return;
            }

            if (required.Any(r => !session.Pages.Contains(r.PageKey, StringComparer.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { detail = "You do not have permission to access this RoyalDesk area." }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}
