namespace RoyalDesk.Api.BusinessValue;

public sealed class BusinessValueSummaryDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int EscalatedTickets { get; set; }
    public double AverageResponseMinutes { get; set; }
    public double AverageResolutionMinutes { get; set; }
    public int TotalStoreUnits { get; set; }
    public int ReservedStoreUnits { get; set; }
    public int LowStockItems { get; set; }
    public int StoreUnitsIssued { get; set; }
    public int TotalAssets { get; set; }
    public int AvailableAssets { get; set; }
    public decimal AvailableAssetValue { get; set; }
    public decimal EstimatedReuseValue { get; set; }
    public IReadOnlyList<BusinessValueBreakdownDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<BusinessValueBreakdownDto> TicketsByCategory { get; set; } = [];
    public IReadOnlyList<TechnicianWorkloadDto> TechnicianWorkload { get; set; } = [];
    public IReadOnlyList<BusinessValueBreakdownDto> StoreMovementsByType { get; set; } = [];
}

public sealed class BusinessValueBreakdownDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class TechnicianWorkloadDto
{
    public string TechnicianName { get; set; } = string.Empty;
    public int Assigned { get; set; }
    public int Active { get; set; }
    public int Resolved { get; set; }
}
