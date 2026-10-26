namespace RoyalDesk.Api.Operations;

public sealed record AssetRequestWorkflowDto(
    int Id, string Branch, string Department, string ItemType, int Quantity, string Reason,
    string RequestedBy, DateTime RequestedAtUtc, string Status, int? EmployeeId,
    string ManagerApprovalStatus, string ITStatus, DateTime? ApprovedAtUtc, DateTime? CompletedAtUtc);

public sealed record EmployeeAccessProfileDto(
    int EmployeeId, string? EmailAddress, string? SapUsername, string? CisTechUsername,
    string? VpnUsername, string? PhoneExtension, bool UserGroupsAdded, bool EmailGroupsAdded,
    bool PrintScanConfigured, bool SharedFoldersConfigured, bool PrintersConfigured,
    string? Notes, string? UpdatedBy, DateTime? UpdatedAtUtc);

public sealed record SaveEmployeeAccessProfileDto(
    string? EmailAddress, string? SapUsername, string? CisTechUsername, string? VpnUsername,
    string? PhoneExtension, bool UserGroupsAdded, bool EmailGroupsAdded, bool PrintScanConfigured,
    bool SharedFoldersConfigured, bool PrintersConfigured, string? Notes);

public sealed record TransferAssetDto(int? BranchId, int? DepartmentId, string Reason);
public sealed record UpdateAssetOperationalStatusDto(string Status, string? Condition, string? Notes);
public sealed record AssetHistoryDto(int Id, int AssetId, string EventType, string Description, string PerformedBy, DateTime CreatedAtUtc);
public sealed record CompleteOffboardingDto(DateTime EndDate);
