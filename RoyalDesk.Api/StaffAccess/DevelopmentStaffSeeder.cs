using Microsoft.Extensions.Configuration;
using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.StaffAccess;

/// <summary>
/// Opt-in local development credentials. Only seeder-owned accounts may be reset;
/// manual staff accounts and production accounts are never modified.
/// </summary>
public static class DevelopmentStaffSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        if (!string.Equals(configuration["ROYALDESK_SEED_DEMO_USERS"], "true", StringComparison.OrdinalIgnoreCase))
            return;

        var access = services.GetRequiredService<StaffAccessService>();
        var existing = await access.GetProfilesAsync(false, ct);
        var reset = string.Equals(configuration["ROYALDESK_RESET_DEMO_PASSWORDS"], "true", StringComparison.OrdinalIgnoreCase);
        var profiles = new[]
        {
            new
            {
                Username = "royaldesk.admin",
                DisplayName = "RoyalDesk Test Administrator",
                Password = configuration["ROYALDESK_DEMO_ADMIN_PASSWORD"],
                Pages = RoyalDeskPages.All.ToList()
            },
            new
            {
                Username = "royaldesk.technician",
                DisplayName = "RoyalDesk Test Technician",
                Password = configuration["ROYALDESK_DEMO_TECH_PASSWORD"],
                Pages = new List<string> { "tickets", "dashboard", "assets" }
            }
        };

        foreach (var profile in profiles)
        {
            var old = existing.SingleOrDefault(x =>
                string.Equals(x.Username, profile.Username, StringComparison.OrdinalIgnoreCase));
            if (old is not null && !reset) continue;
            if (string.IsNullOrWhiteSpace(profile.Password) || profile.Password.Length < 12)
                throw new InvalidOperationException(
                    $"A development password of at least 12 characters is required for {profile.Username}.");

            if (old is not null)
            {
                var factory = services.GetRequiredService<IDbConnectionFactory>();
                await using var db = factory.CreateConnection();
                await db.OpenAsync(ct);
                var owned = await db.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT COUNT(*) FROM StaffPortalAccess WHERE Id=@id AND CreatedBy='local-development-seeder'",
                    new { id = old.Id }, cancellationToken: ct));
                if (owned != 1)
                    throw new InvalidOperationException(
                        $"Refusing to reset {profile.Username}; this account was not created by the development seeder.");
                await access.ResetPasswordAsync(old.Id, profile.Password, ct);
                continue;
            }

            await access.CreateAsync(new CreateStaffAccessDto
            {
                DisplayName = profile.DisplayName,
                Username = profile.Username,
                Password = profile.Password,
                Pages = profile.Pages
            }, "local-development-seeder", ct);
        }
    }
}
