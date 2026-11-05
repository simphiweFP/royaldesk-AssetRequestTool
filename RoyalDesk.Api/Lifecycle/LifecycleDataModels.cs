namespace RoyalDesk.Api.Lifecycle;

// Internal persistence models. These types reflect SQLite/Dapper materialisation
// and must never be exposed directly by API controllers.
internal sealed class LookupRecord
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

internal sealed class DepartmentRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class EmployeeRecord
{
    public long Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? JobTitle { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

internal sealed class AssetRecord
{
    public long Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? PurchaseDate { get; set; }
    public string? PurchaseValue { get; set; }
    public string? WarrantyExpiry { get; set; }
    public long FullyDepreciated { get; set; }
    public string? Notes { get; set; }
    public long? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public long? StoreItemId { get; set; }
    public string? StoreItemName { get; set; }
    public long PhysicallyInStore { get; set; }
}

internal sealed class AssignmentRecord
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string AssignedAtUtc { get; set; } = string.Empty;
    public string AssignedBy { get; set; } = string.Empty;
    public string ConditionAtAssignment { get; set; } = string.Empty;
    public string? ReturnedAtUtc { get; set; }
    public string? ReturnReason { get; set; }
    public string? ConditionAtReturn { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EmployeeAcceptedAtUtc { get; set; }
    public string? EmployeeSignatureName { get; set; }
}

internal sealed class LifecycleTaskRecord
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string LifecycleType { get; set; } = string.Empty;
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public long IsCompleted { get; set; }
    public string? CompletedBy { get; set; }
    public string? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

internal sealed class WriteOffRecord
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public long FullyDepreciated { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedAtUtc { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string? ApprovedAtUtc { get; set; }
    public string? DisposalMethod { get; set; }
    public string? DisposalCompany { get; set; }
    public string? DisposalDate { get; set; }
    public string? CertificateReference { get; set; }
    public string? Notes { get; set; }
}

internal sealed class DashboardRecord
{
    public long TotalAssets { get; set; }
    public long AssignedAssets { get; set; }
    public long AvailableAssets { get; set; }
    public long InRepairAssets { get; set; }
    public long PendingWriteOffs { get; set; }
    public string TotalAssetValue { get; set; } = "0";
    public string AvailableAssetValue { get; set; } = "0";
    public long PendingRequests { get; set; }
    public long ActiveEmployees { get; set; }
    public string EstimatedReuseValue { get; set; } = "0";
}

internal sealed record CreateEmployeeCommand(
    string EmployeeNumber, string FirstName, string LastName, string? Email,
    string? Username, string? JobTitle, int? BranchId, int? DepartmentId,
    DateTime? StartDate);

internal sealed record CreateAssetCommand(
    string AssetTag, string? SerialNumber, string Category, string? Make,
    string? Model, int? BranchId, int? DepartmentId, string Condition,
    DateTime? PurchaseDate, decimal? PurchaseValue, DateTime? WarrantyExpiry,
    bool FullyDepreciated, string? Notes);

internal sealed record AssignAssetCommand(int AssetId, int EmployeeId, string Condition);
internal sealed record AcceptAssetCommand(string SignatureName);
internal sealed record ReturnAssetCommand(string Reason, string Condition, string? Notes);
internal sealed record UpdateLifecycleTaskCommand(bool IsCompleted, string? Notes);
internal sealed record CreateWriteOffCommand(int AssetId, string Reason, string Condition, decimal? CurrentValue, string? Notes);
internal sealed record ApproveWriteOffCommand(bool Approved, string? Notes);
internal sealed record DisposeWriteOffCommand(string DisposalMethod, string? DisposalCompany, DateTime DisposalDate, string? CertificateReference, string? Notes);
internal sealed record UpdateRequestWorkflowCommand(string Status, string? ManagerApprovalStatus, string? ITStatus);
internal sealed record ImportEwasteRowCommand(
    string SerialNumber, string? Make, string? Model, string Category, string Condition,
    decimal? Value, bool FullyDepreciated, string? DisposalMethod, DateTime? DisposalDate,
    string? ApprovedBy, string? Notes);

internal sealed record ImportResultRecord(int Imported, int Skipped, IReadOnlyList<string> Warnings);
