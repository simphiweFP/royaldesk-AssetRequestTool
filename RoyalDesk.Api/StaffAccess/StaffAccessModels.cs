namespace RoyalDesk.Api.StaffAccess;

public static class RoyalDeskPages
{
    public static readonly string[] All =
    [
        "dashboard", "tickets", "store", "assets", "assignments", "employees",
        "onboarding", "offboarding", "writeoffs", "business-value", "access-management"
    ];
}

public sealed class StaffProfileDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastAccessAtUtc { get; set; }
    public List<string> Pages { get; set; } = [];
}

public sealed class StaffSessionDto
{
    public int StaffAccessId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public List<string> Pages { get; set; } = [];
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class UnlockStaffDto
{
    public int StaffAccessId { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateStaffAccessDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Password { get; set; } = string.Empty;
    public List<string> Pages { get; set; } = [];
}

public sealed class UpdateStaffAccessDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public bool IsActive { get; set; }
    public List<string> Pages { get; set; } = [];
}

public sealed class ResetStaffPasswordDto
{
    public string Password { get; set; } = string.Empty;
}

public sealed class BootstrapStaffDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Password { get; set; } = string.Empty;
    public string BootstrapKey { get; set; } = string.Empty;
}
