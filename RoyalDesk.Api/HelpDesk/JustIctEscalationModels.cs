namespace RoyalDesk.Api.HelpDesk;

public sealed class EscalateToJustIctDto
{
    public string TeamViewerId { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string Troubleshooting { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }
    public string EmailBody { get; set; } = string.Empty;
}

public sealed class TicketEscalationDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string TeamViewerId { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string Troubleshooting { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }
    public string EscalatedByName { get; set; } = string.Empty;
    public DateTime EscalatedAtUtc { get; set; }
}
