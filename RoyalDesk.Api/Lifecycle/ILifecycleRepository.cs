namespace RoyalDesk.Api.Lifecycle;

public interface ILifecycleRepository
{
    Task<IReadOnlyList<LookupDto>> GetBranchesAsync(CancellationToken ct);
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken ct);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken ct);
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct);
    Task<AssetDto> CreateAssetAsync(CreateAssetDto dto, string actor, CancellationToken ct);
    Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(int? employeeId, CancellationToken ct);
    Task<AssignmentDto> AssignAssetAsync(AssignAssetDto dto, string actor, CancellationToken ct);
    Task AcceptAssetAsync(int assignmentId, AcceptAssetDto dto, string actor, CancellationToken ct);
    Task ReturnAssetAsync(int assignmentId, ReturnAssetDto dto, string actor, CancellationToken ct);
    Task<IReadOnlyList<LifecycleTaskDto>> GetTasksAsync(int employeeId, string lifecycleType, CancellationToken ct);
    Task EnsureTasksAsync(int employeeId, string lifecycleType, CancellationToken ct);
    Task UpdateTaskAsync(int taskId, UpdateLifecycleTaskDto dto, string actor, CancellationToken ct);
    Task<IReadOnlyList<WriteOffDto>> GetWriteOffsAsync(CancellationToken ct);
    Task<WriteOffDto> CreateWriteOffAsync(CreateWriteOffDto dto, string actor, CancellationToken ct);
    Task ApproveWriteOffAsync(int id, ApproveWriteOffDto dto, string actor, CancellationToken ct);
    Task DisposeWriteOffAsync(int id, DisposeWriteOffDto dto, string actor, CancellationToken ct);
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct);
    Task UpdateRequestWorkflowAsync(int id, UpdateRequestWorkflowDto dto, string actor, CancellationToken ct);
    Task<AssignmentDto?> GetAssignmentAsync(int id, CancellationToken ct);
    Task<ImportResultDto> ImportEwasteAsync(IReadOnlyList<ImportEwasteRowDto> rows, string actor, CancellationToken ct);
}
