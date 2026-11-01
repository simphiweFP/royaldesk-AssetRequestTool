namespace RoyalDesk.Api.DTOs;

public sealed class AssetRequestResponseDto
{
    public int Id { get; init; }
    public string TicketNumber { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterEmail { get; init; }
    public string? TrackingPin { get; init; }
    public string Branch { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = "New";
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; }
}
