using Microsoft.AspNetCore.Authentication;

namespace RoyalDesk.Api.Authentication;

public sealed class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Realm { get; set; } = "RoyalDesk";
}
