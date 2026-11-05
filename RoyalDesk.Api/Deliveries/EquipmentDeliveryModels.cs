namespace RoyalDesk.Api.Deliveries;

public sealed class EquipmentDeliveryDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = "";
    public int StoreItemId { get; set; }
    public string? StoreItemName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetTag { get; set; }
    public string DestinationBranch { get; set; } = "";
    public bool DeliveryRequired { get; set; }
    public string RecipientName { get; set; } = "";
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public string Status { get; set; } = "";
    public string? DriverName { get; set; }
    public string? TrackingReference { get; set; }
    public string? ReceivedBy { get; set; }
    public string? ReceiptReference { get; set; }
    public string? ExceptionNotes { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? PickedUpAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class EquipmentDeliveryEventDto
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public int? Quantity { get; set; }
    public string? Details { get; set; }
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAtUtc { get; set; }
}

public sealed record PrepareEquipmentDeliveryDto(int StoreItemId, int? AssetId, bool DeliveryRequired,
    string RecipientName, string? LocalHandoverReason);
public sealed record PickupEquipmentDeliveryDto(string DriverName, string? TrackingReference);
public sealed record ConfirmEquipmentReceiptDto(int Quantity, string ReceivedBy, string ReceiptReference, string? Notes);
public sealed record DeliveryExceptionDto(string Notes);
