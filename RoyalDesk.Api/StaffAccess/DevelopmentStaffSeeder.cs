using Microsoft.Extensions.Configuration;

namespace RoyalDesk.Api.StaffAccess;

/// <summary>
/// Opt-in LOCAL DEVELOPMENT ONLY. Creates test staff profiles with passwords
/// supplied through environment variables; never seeds credentials in production.
/// Existing profiles are left untouched, including passwords.
/// </summary>
public static class DevelopmentStaffSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        var enabled = string.Equals(configuration["ROYALDESK_SEED_DEMO_USERS"], "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled) return;

        var access = services.GetRequiredService<StaffAccessService>();
        var existing = await access.GetProfilesAsync(false, ct);
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
            if (existing.Any(x => string.Equals(x.Username, profile.Username, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (string.IsNullOrWhiteSpace(profile.Password) || profile.Password.Length < 12)
                throw new InvalidOperationException(
                    $"Development seeding needs a 12+ character password for {profile.Username}. Set the corresponding environment variable.");

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
