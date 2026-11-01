namespace RoyalDesk.Api.Lifecycle;

internal interface ILifecycleRepository
{
    Task<IReadOnlyList<LookupRecord>> GetBranchesAsync(CancellationToken ct);
    Task<IReadOnlyList<DepartmentRecord>> GetDepartmentsAsync(CancellationToken ct);
    Task<IReadOnlyList<EmployeeRecord>> GetEmployeesAsync(CancellationToken ct);
    Task<EmployeeRecord> CreateEmployeeAsync(CreateEmployeeCommand command, CancellationToken ct);
    Task<IReadOnlyList<AssetRecord>> GetAssetsAsync(CancellationToken ct);
    Task<AssetRecord> CreateAssetAsync(CreateAssetCommand command, string actor, CancellationToken ct);
    Task<IReadOnlyList<AssignmentRecord>> GetAssignmentsAsync(int? employeeId, CancellationToken ct);
    Task<AssignmentRecord> AssignAssetAsync(AssignAssetCommand command, string actor, CancellationToken ct);
    Task AcceptAssetAsync(int assignmentId, AcceptAssetCommand command, string actor, CancellationToken ct);
    Task ReturnAssetAsync(int assignmentId, ReturnAssetCommand command, string actor, CancellationToken ct);
    Task<IReadOnlyList<LifecycleTaskRecord>> GetTasksAsync(int employeeId, string lifecycleType, CancellationToken ct);
    Task EnsureTasksAsync(int employeeId, string lifecycleType, CancellationToken ct);
    Task UpdateTaskAsync(int taskId, UpdateLifecycleTaskCommand command, string actor, CancellationToken ct);
    Task<IReadOnlyList<WriteOffRecord>> GetWriteOffsAsync(CancellationToken ct);
    Task<WriteOffRecord> CreateWriteOffAsync(CreateWriteOffCommand command, string actor, CancellationToken ct);
    Task ApproveWriteOffAsync(int id, ApproveWriteOffCommand command, string actor, CancellationToken ct);
    Task DisposeWriteOffAsync(int id, DisposeWriteOffCommand command, string actor, CancellationToken ct);
    Task<DashboardRecord> GetDashboardAsync(CancellationToken ct);
    Task UpdateRequestWorkflowAsync(int id, UpdateRequestWorkflowCommand command, string actor, CancellationToken ct);
    Task<AssignmentRecord?> GetAssignmentAsync(int id, CancellationToken ct);
    Task<ImportResultRecord> ImportEwasteAsync(IReadOnlyList<ImportEwasteRowCommand> rows, string actor, CancellationToken ct);
}
