namespace RoyalDesk.Api.Lifecycle;

public sealed class LookupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? JobTitle { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed record CreateEmployeeDto(
    string EmployeeNumber, string FirstName, string LastName, string? Email,
    string? Username, string? JobTitle, int? BranchId, int? DepartmentId,
    DateTime? StartDate);

public sealed class AssetDto
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public bool FullyDepreciated { get; set; }
    public string? Notes { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
}

public sealed record CreateAssetDto(
    string AssetTag, string? SerialNumber, string Category, string? Make,
    string? Model, int? BranchId, int? DepartmentId, string Condition,
    DateTime? PurchaseDate, decimal? PurchaseValue, DateTime? WarrantyExpiry,
    bool FullyDepreciated, string? Notes);

public sealed class AssignmentDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string ConditionAtAssignment { get; set; } = string.Empty;
    public DateTime? ReturnedAtUtc { get; set; }
    public string? ReturnReason { get; set; }
    public string? ConditionAtReturn { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? EmployeeAcceptedAtUtc { get; set; }
    public string? EmployeeSignatureName { get; set; }
}

public sealed record AssignAssetDto(int AssetId, int EmployeeId, string Condition);
public sealed record AcceptAssetDto(string SignatureName);
public sealed record ReturnAssetDto(string Reason, string Condition, string? Notes);

public sealed class LifecycleTaskDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string LifecycleType { get; set; } = string.Empty;
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string? CompletedBy { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed record UpdateLifecycleTaskDto(bool IsCompleted, string? Notes);

public sealed class WriteOffDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal? CurrentValue { get; set; }
    public bool FullyDepreciated { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? DisposalMethod { get; set; }
    public string? DisposalCompany { get; set; }
    public DateTime? DisposalDate { get; set; }
    public string? CertificateReference { get; set; }
    public string? Notes { get; set; }
}

public sealed record CreateWriteOffDto(int AssetId, string Reason, string Condition, decimal? CurrentValue, string? Notes);
public sealed record ApproveWriteOffDto(bool Approved, string? Notes);
public sealed record DisposeWriteOffDto(string DisposalMethod, string? DisposalCompany, DateTime DisposalDate, string? CertificateReference, string? Notes);

public sealed class DashboardDto
{
    public int TotalAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int AvailableAssets { get; set; }
    public int InRepairAssets { get; set; }
    public int PendingWriteOffs { get; set; }
    public decimal TotalAssetValue { get; set; }
    public decimal AvailableAssetValue { get; set; }
    public int PendingRequests { get; set; }
    public int ActiveEmployees { get; set; }
    public decimal EstimatedReuseValue { get; set; }
}

public sealed record UpdateRequestWorkflowDto(string Status, string? ManagerApprovalStatus, string? ITStatus);

public sealed record ImportEwasteRowDto(
    string SerialNumber, string? Make, string? Model, string Category, string Condition,
    decimal? Value, bool FullyDepreciated, string? DisposalMethod, DateTime? DisposalDate,
    string? ApprovedBy, string? Notes);

public sealed record ImportResultDto(int Imported, int Skipped, IReadOnlyList<string> Warnings);
