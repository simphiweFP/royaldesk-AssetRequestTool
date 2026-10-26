namespace RoyalDesk.Api.Lifecycle;

public sealed record LookupDto(int Id, string Code, string Name);
public sealed record DepartmentDto(int Id, string Name);

public sealed record EmployeeDto(
    int Id, string EmployeeNumber, string FirstName, string LastName, string? Email,
    string? Username, string? JobTitle, int? BranchId, string? BranchName,
    int? DepartmentId, string? DepartmentName, string EmploymentStatus,
    DateTime? StartDate, DateTime? EndDate);

public sealed record CreateEmployeeDto(
    string EmployeeNumber, string FirstName, string LastName, string? Email,
    string? Username, string? JobTitle, int? BranchId, int? DepartmentId,
    DateTime? StartDate);

public sealed record AssetDto(
    int Id, string AssetTag, string? SerialNumber, string Category, string? Make,
    string? Model, int? BranchId, string? BranchName, int? DepartmentId,
    string? DepartmentName, string Status, string Condition, DateTime? PurchaseDate,
    decimal? PurchaseValue, DateTime? WarrantyExpiry, bool FullyDepreciated,
    string? Notes, int? AssignedEmployeeId, string? AssignedEmployeeName);

public sealed record CreateAssetDto(
    string AssetTag, string? SerialNumber, string Category, string? Make,
    string? Model, int? BranchId, int? DepartmentId, string Condition,
    DateTime? PurchaseDate, decimal? PurchaseValue, DateTime? WarrantyExpiry,
    bool FullyDepreciated, string? Notes);

public sealed record AssignmentDto(
    int Id, int AssetId, string AssetTag, int EmployeeId, string EmployeeName,
    DateTime AssignedAtUtc, string AssignedBy, string ConditionAtAssignment,
    DateTime? ReturnedAtUtc, string? ReturnReason, string? ConditionAtReturn,
    string Status, DateTime? EmployeeAcceptedAtUtc, string? EmployeeSignatureName);

public sealed record AssignAssetDto(int AssetId, int EmployeeId, string Condition);
public sealed record AcceptAssetDto(string SignatureName);
public sealed record ReturnAssetDto(string Reason, string Condition, string? Notes);

public sealed record LifecycleTaskDto(
    int Id, int EmployeeId, string LifecycleType, string TaskCode, string TaskName,
    bool IsCompleted, string? CompletedBy, DateTime? CompletedAtUtc, string? Notes);

public sealed record UpdateLifecycleTaskDto(bool IsCompleted, string? Notes);

public sealed record WriteOffDto(
    int Id, int AssetId, string AssetTag, string? SerialNumber, string? Make,
    string? Model, string Category, string Condition, decimal? CurrentValue,
    bool FullyDepreciated, string Reason, string RequestedBy, DateTime RequestedAtUtc,
    string ApprovalStatus, string? ApprovedBy, DateTime? ApprovedAtUtc,
    string? DisposalMethod, string? DisposalCompany, DateTime? DisposalDate,
    string? CertificateReference, string? Notes);

public sealed record CreateWriteOffDto(int AssetId, string Reason, string Condition, decimal? CurrentValue, string? Notes);
public sealed record ApproveWriteOffDto(bool Approved, string? Notes);
public sealed record DisposeWriteOffDto(string DisposalMethod, string? DisposalCompany, DateTime DisposalDate, string? CertificateReference, string? Notes);

public sealed record DashboardDto(
    int TotalAssets, int AssignedAssets, int AvailableAssets, int InRepairAssets,
    int PendingWriteOffs, decimal TotalAssetValue, decimal AvailableAssetValue,
    int PendingRequests, int ActiveEmployees, decimal EstimatedReuseValue);

public sealed record UpdateRequestWorkflowDto(string Status, string? ManagerApprovalStatus, string? ITStatus);

public sealed record ImportEwasteRowDto(
    string SerialNumber, string? Make, string? Model, string Category, string Condition,
    decimal? Value, bool FullyDepreciated, string? DisposalMethod, DateTime? DisposalDate,
    string? ApprovedBy, string? Notes);

public sealed record ImportResultDto(int Imported, int Skipped, IReadOnlyList<string> Warnings);
