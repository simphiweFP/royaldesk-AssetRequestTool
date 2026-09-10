using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace RoyalDesk.Api.Authentication;

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<BasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<BasicAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "Basic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var value))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(value, out var header) ||
            !SchemeName.Equals(header.Scheme, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));

        try
        {
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            var separator = credentials.IndexOf(':');

            if (separator <= 0)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials."));

            var username = credentials[..separator];
            var password = credentials[(separator + 1)..];

            if (!Matches(username, Options.Username) || !Matches(password, Options.Password))
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, username), new Claim(ClaimTypes.Name, username)],
                SchemeName);

            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials."));
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"" + Options.Realm + "\"";
        await base.HandleChallengeAsync(properties);
    }

    private static bool Matches(string supplied, string configured) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied),
            Encoding.UTF8.GetBytes(configured));
}
