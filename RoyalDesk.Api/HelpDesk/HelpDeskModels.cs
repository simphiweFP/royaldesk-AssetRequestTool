namespace RoyalDesk.Api.HelpDesk;

public sealed class ITTeamMemberDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class HelpDeskTicketDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
}

public sealed class TicketStatusHistoryDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Comment { get; set; }
    public bool PublicVisible { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}

public sealed class CreateITTeamMemberDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "IT Technician";
}

public sealed class UpdateTicketStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public bool PublicVisible { get; set; } = true;
}
