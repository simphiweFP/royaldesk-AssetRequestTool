namespace RoyalDesk.Api.Models;

public sealed class AssetRequest
{
    public int Id { get; set; }
    public string? TicketNumber { get; set; }
    public string RequestType { get; set; } = "Equipment";
    public string RequesterName { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string? TrackingPinHash { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
}
