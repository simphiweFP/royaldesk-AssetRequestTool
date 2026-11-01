namespace RoyalDesk.Api.Lifecycle;

public sealed class LifecycleService(ILifecycleRepository repository) : ILifecycleService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
        => (await repository.GetDashboardAsync(ct)).ToDto();

    public async Task<IReadOnlyList<LookupDto>> GetBranchesAsync(CancellationToken ct)
        => (await repository.GetBranchesAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct)
        => (await repository.GetDepartmentsAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken ct)
        => (await repository.GetEmployeesAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken ct)
    {
        var command = new CreateEmployeeCommand(
            dto.EmployeeNumber, dto.FirstName, dto.LastName, dto.Email,
            dto.Username, dto.JobTitle, dto.BranchId, dto.DepartmentId, dto.StartDate);
        return (await repository.CreateEmployeeAsync(command, ct)).ToDto();
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct)
        => (await repository.GetAssetsAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto, string actor, CancellationToken ct)
    {
        var command = new CreateAssetCommand(
            dto.AssetTag, dto.SerialNumber, dto.Category, dto.Make, dto.Model,
            dto.BranchId, dto.DepartmentId, dto.Condition, dto.PurchaseDate,
            dto.PurchaseValue, dto.WarrantyExpiry, dto.FullyDepreciated, dto.Notes);
        return (await repository.CreateAssetAsync(command, actor, ct)).ToDto();
    }

    public async Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(int? employeeId, CancellationToken ct)
        => (await repository.GetAssignmentsAsync(employeeId, ct)).Select(x => x.ToDto()).ToList();

    public async Task<AssignmentDto> AssignAssetAsync(AssignAssetDto dto, string actor, CancellationToken ct)
    {
        var command = new AssignAssetCommand(dto.AssetId, dto.EmployeeId, dto.Condition);
        return (await repository.AssignAssetAsync(command, actor, ct)).ToDto();
    }

    public Task AcceptAssetAsync(int assignmentId, AcceptAssetDto dto, string actor, CancellationToken ct)
        => repository.AcceptAssetAsync(assignmentId, new AcceptAssetCommand(dto.SignatureName), actor, ct);

    public Task ReturnAssetAsync(int assignmentId, ReturnAssetDto dto, string actor, CancellationToken ct)
        => repository.ReturnAssetAsync(assignmentId, new ReturnAssetCommand(dto.Reason, dto.Condition, dto.Notes), actor, ct);

    public async Task<AssignmentDto?> GetAssignmentAsync(int id, CancellationToken ct)
        => (await repository.GetAssignmentAsync(id, ct))?.ToDto();

    public async Task<IReadOnlyList<LifecycleTaskDto>> GetTasksAsync(int employeeId, string lifecycleType, CancellationToken ct)
        => (await repository.GetTasksAsync(employeeId, lifecycleType, ct)).Select(x => x.ToDto()).ToList();

    public Task UpdateTaskAsync(int taskId, UpdateLifecycleTaskDto dto, string actor, CancellationToken ct)
        => repository.UpdateTaskAsync(taskId, new UpdateLifecycleTaskCommand(dto.IsCompleted, dto.Notes), actor, ct);

    public async Task<IReadOnlyList<WriteOffDto>> GetWriteOffsAsync(CancellationToken ct)
        => (await repository.GetWriteOffsAsync(ct)).Select(x => x.ToDto()).ToList();

    public async Task<WriteOffDto> CreateWriteOffAsync(CreateWriteOffDto dto, string actor, CancellationToken ct)
    {
        var command = new CreateWriteOffCommand(dto.AssetId, dto.Reason, dto.Condition, dto.CurrentValue, dto.Notes);
        return (await repository.CreateWriteOffAsync(command, actor, ct)).ToDto();
    }

    public Task ApproveWriteOffAsync(int id, ApproveWriteOffDto dto, string actor, CancellationToken ct)
        => repository.ApproveWriteOffAsync(id, new ApproveWriteOffCommand(dto.Approved, dto.Notes), actor, ct);

    public Task DisposeWriteOffAsync(int id, DisposeWriteOffDto dto, string actor, CancellationToken ct)
        => repository.DisposeWriteOffAsync(id, new DisposeWriteOffCommand(dto.DisposalMethod, dto.DisposalCompany, dto.DisposalDate, dto.CertificateReference, dto.Notes), actor, ct);

    public Task UpdateRequestWorkflowAsync(int id, UpdateRequestWorkflowDto dto, string actor, CancellationToken ct)
        => repository.UpdateRequestWorkflowAsync(id, new UpdateRequestWorkflowCommand(dto.Status, dto.ManagerApprovalStatus, dto.ITStatus), actor, ct);

    public async Task<ImportResultDto> ImportEwasteAsync(IReadOnlyList<ImportEwasteRowDto> rows, string actor, CancellationToken ct)
    {
        var commands = rows.Select(row => new ImportEwasteRowCommand(
            row.SerialNumber, row.Make, row.Model, row.Category, row.Condition,
            row.Value, row.FullyDepreciated, row.DisposalMethod, row.DisposalDate,
            row.ApprovedBy, row.Notes)).ToList();

        var result = await repository.ImportEwasteAsync(commands, actor, ct);
        return new ImportResultDto(result.Imported, result.Skipped, result.Warnings);
    }
}
