using System.Globalization;

namespace RoyalDesk.Api.Lifecycle;

internal static class LifecycleMappings
{
    public static LookupDto ToDto(this LookupRecord row) => new()
    {
        Id = ToInt(row.Id),
        Code = row.Code,
        Name = row.Name
    };

    public static DepartmentDto ToDto(this DepartmentRecord row) => new()
    {
        Id = ToInt(row.Id),
        Name = row.Name
    };

    public static EmployeeDto ToDto(this EmployeeRecord row) => new()
    {
        Id = ToInt(row.Id),
        EmployeeNumber = row.EmployeeNumber,
        FirstName = row.FirstName,
        LastName = row.LastName,
        Email = row.Email,
        Username = row.Username,
        JobTitle = row.JobTitle,
        BranchId = ToNullableInt(row.BranchId),
        BranchName = row.BranchName,
        DepartmentId = ToNullableInt(row.DepartmentId),
        DepartmentName = row.DepartmentName,
        EmploymentStatus = row.EmploymentStatus,
        StartDate = ToNullableDateTime(row.StartDate),
        EndDate = ToNullableDateTime(row.EndDate)
    };

    public static AssetDto ToDto(this AssetRecord row) => new()
    {
        Id = ToInt(row.Id),
        AssetTag = row.AssetTag,
        SerialNumber = row.SerialNumber,
        Category = row.Category,
        Make = row.Make,
        Model = row.Model,
        BranchId = ToNullableInt(row.BranchId),
        BranchName = row.BranchName,
        DepartmentId = ToNullableInt(row.DepartmentId),
        DepartmentName = row.DepartmentName,
        Status = row.Status,
        Condition = row.Condition,
        PurchaseDate = ToNullableDateTime(row.PurchaseDate),
        PurchaseValue = ToNullableDecimal(row.PurchaseValue),
        WarrantyExpiry = ToNullableDateTime(row.WarrantyExpiry),
        FullyDepreciated = row.FullyDepreciated != 0,
        Notes = row.Notes,
        AssignedEmployeeId = ToNullableInt(row.AssignedEmployeeId),
        AssignedEmployeeName = row.AssignedEmployeeName,
        StoreItemId = ToNullableInt(row.StoreItemId),
        StoreItemName = row.StoreItemName,
        PhysicallyInStore = row.PhysicallyInStore != 0
    };

    public static AssignmentDto ToDto(this AssignmentRecord row) => new()
    {
        Id = ToInt(row.Id),
        AssetId = ToInt(row.AssetId),
        AssetTag = row.AssetTag,
        EmployeeId = ToInt(row.EmployeeId),
        EmployeeName = row.EmployeeName,
        AssignedAtUtc = ToRequiredDateTime(row.AssignedAtUtc),
        AssignedBy = row.AssignedBy,
        ConditionAtAssignment = row.ConditionAtAssignment,
        ReturnedAtUtc = ToNullableDateTime(row.ReturnedAtUtc),
        ReturnReason = row.ReturnReason,
        ConditionAtReturn = row.ConditionAtReturn,
        Status = row.Status,
        EmployeeAcceptedAtUtc = ToNullableDateTime(row.EmployeeAcceptedAtUtc),
        EmployeeSignatureName = row.EmployeeSignatureName
    };

    public static LifecycleTaskDto ToDto(this LifecycleTaskRecord row) => new()
    {
        Id = ToInt(row.Id),
        EmployeeId = ToInt(row.EmployeeId),
        LifecycleType = row.LifecycleType,
        TaskCode = row.TaskCode,
        TaskName = row.TaskName,
        IsCompleted = row.IsCompleted != 0,
        CompletedBy = row.CompletedBy,
        CompletedAtUtc = ToNullableDateTime(row.CompletedAtUtc),
        Notes = row.Notes
    };

    public static WriteOffDto ToDto(this WriteOffRecord row) => new()
    {
        Id = ToInt(row.Id),
        AssetId = ToInt(row.AssetId),
        AssetTag = row.AssetTag,
        SerialNumber = row.SerialNumber,
        Make = row.Make,
        Model = row.Model,
        Category = row.Category,
        Condition = row.Condition,
        CurrentValue = ToNullableDecimal(row.CurrentValue),
        FullyDepreciated = row.FullyDepreciated != 0,
        Reason = row.Reason,
        RequestedBy = row.RequestedBy,
        RequestedAtUtc = ToRequiredDateTime(row.RequestedAtUtc),
        ApprovalStatus = row.ApprovalStatus,
        ApprovedBy = row.ApprovedBy,
        ApprovedAtUtc = ToNullableDateTime(row.ApprovedAtUtc),
        DisposalMethod = row.DisposalMethod,
        DisposalCompany = row.DisposalCompany,
        DisposalDate = ToNullableDateTime(row.DisposalDate),
        CertificateReference = row.CertificateReference,
        Notes = row.Notes
    };

    public static DashboardDto ToDto(this DashboardRecord row) => new()
    {
        TotalAssets = ToInt(row.TotalAssets),
        AssignedAssets = ToInt(row.AssignedAssets),
        AvailableAssets = ToInt(row.AvailableAssets),
        InRepairAssets = ToInt(row.InRepairAssets),
        PendingWriteOffs = ToInt(row.PendingWriteOffs),
        TotalAssetValue = ToDecimal(row.TotalAssetValue),
        AvailableAssetValue = ToDecimal(row.AvailableAssetValue),
        PendingRequests = ToInt(row.PendingRequests),
        ActiveEmployees = ToInt(row.ActiveEmployees),
        EstimatedReuseValue = ToDecimal(row.EstimatedReuseValue)
    };

    private static int ToInt(long value) => checked((int)value);
    private static int? ToNullableInt(long? value) => value.HasValue ? ToInt(value.Value) : null;

    private static DateTime ToRequiredDateTime(string value)
        => ToNullableDateTime(value) ?? throw new InvalidOperationException($"Invalid persisted date value '{value}'.");

    private static DateTime? ToNullableDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) return parsed;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed)) return parsed;
        throw new InvalidOperationException($"Invalid persisted date value '{value}'.");
    }

    private static decimal? ToNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        throw new InvalidOperationException($"Invalid persisted decimal value '{value}'.");
    }

    private static decimal ToDecimal(string? value) => ToNullableDecimal(value) ?? 0m;
}
