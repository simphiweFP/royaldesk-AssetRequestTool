namespace RoyalDesk.Api.StoreRoom;

public sealed class StoreItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Location { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int MinimumStock { get; set; }
    public bool IsAssetTracked { get; set; }
    public bool IsActive { get; set; }
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
    public string StockStatus => AvailableQuantity <= MinimumStock ? "Low Stock" : "Available";
}

public sealed class StoreMovementDto
{
    public int Id { get; set; }
    public int StoreItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int? TicketId { get; set; }
    public string? TicketNumber { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAtUtc { get; set; }
}

public sealed class CreateStoreItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Location { get; set; }
    public int OpeningQuantity { get; set; }
    public int MinimumStock { get; set; }
    public bool IsAssetTracked { get; set; }
}

public sealed class StoreMovementRequest
{
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? TicketId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
