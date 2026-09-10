namespace RoyalDesk.Api.DTOs;

public sealed class CreateAssetRequestDto
{
    public string Branch { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
