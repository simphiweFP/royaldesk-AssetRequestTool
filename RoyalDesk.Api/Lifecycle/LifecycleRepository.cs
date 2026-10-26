using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.Lifecycle;

public sealed class LifecycleRepository(IDbConnectionFactory connectionFactory) : ILifecycleRepository
{
    public async Task<IReadOnlyList<LookupDto>> GetBranchesAsync(CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<LookupDto>(new CommandDefinition("SELECT Id, Code, Name FROM Branches WHERE IsActive = 1 ORDER BY Name", cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<DepartmentDto>(new CommandDefinition("SELECT Id, Name FROM Departments WHERE IsActive = 1 ORDER BY Name", cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.Email, e.Username, e.JobTitle,
                   e.BranchId, b.Name BranchName, e.DepartmentId, d.Name DepartmentName,
                   e.EmploymentStatus, e.StartDate, e.EndDate
            FROM Employees e
            LEFT JOIN Branches b ON b.Id = e.BranchId
            LEFT JOIN Departments d ON d.Id = e.DepartmentId
            ORDER BY e.LastName, e.FirstName;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<EmployeeDto>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO Employees(EmployeeNumber, FirstName, LastName, Email, Username, JobTitle, BranchId, DepartmentId, EmploymentStatus, StartDate, CreatedAtUtc, UpdatedAtUtc)
            VALUES(@EmployeeNumber,@FirstName,@LastName,@Email,@Username,@JobTitle,@BranchId,@DepartmentId,'Active',@StartDate,@Now,@Now);
            SELECT last_insert_rowid();
            """;
        await using var db = connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(insert, new { dto.EmployeeNumber, dto.FirstName, dto.LastName, dto.Email, dto.Username, dto.JobTitle, dto.BranchId, dto.DepartmentId, dto.StartDate, Now = DateTime.UtcNow }, cancellationToken: ct));
        var employees = await GetEmployeesAsync(ct);
        return employees.Single(x => x.Id == id);
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT a.Id, a.AssetTag, a.SerialNumber, a.Category, a.Make, a.Model, a.BranchId,
                   b.Name BranchName, a.DepartmentId, d.Name DepartmentName, a.Status, a.Condition,
                   a.PurchaseDate, a.PurchaseValue, a.WarrantyExpiry, a.FullyDepreciated, a.Notes,
                   aa.EmployeeId AssignedEmployeeId,
                   CASE WHEN e.Id IS NULL THEN NULL ELSE e.FirstName || ' ' || e.LastName END AssignedEmployeeName
            FROM Assets a
            LEFT JOIN Branches b ON b.Id = a.BranchId
            LEFT JOIN Departments d ON d.Id = a.DepartmentId
            LEFT JOIN AssetAssignments aa ON aa.AssetId = a.Id AND aa.Status = 'Assigned'
            LEFT JOIN Employees e ON e.Id = aa.EmployeeId
            ORDER BY a.AssetTag;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<AssetDto>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto, string actor, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Assets(AssetTag,SerialNumber,Category,Make,Model,BranchId,DepartmentId,Status,Condition,PurchaseDate,PurchaseValue,WarrantyExpiry,FullyDepreciated,Notes,CreatedAtUtc,UpdatedAtUtc)
            VALUES(@AssetTag,@SerialNumber,@Category,@Make,@Model,@BranchId,@DepartmentId,'Available',@Condition,@PurchaseDate,@PurchaseValue,@WarrantyExpiry,@FullyDepreciated,@Notes,@Now,@Now);
            SELECT last_insert_rowid();
            """;
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { dto.AssetTag, dto.SerialNumber, dto.Category, dto.Make, dto.Model, dto.BranchId, dto.DepartmentId, dto.Condition, dto.PurchaseDate, dto.PurchaseValue, dto.WarrantyExpiry, dto.FullyDepreciated, dto.Notes, Now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@id,'Created','Asset registered',@actor,@now)", new { id, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return (await GetAssetsAsync(ct)).Single(x => x.Id == id);
    }

    public async Task<IReadOnlyList<AssignmentDto>> GetAssignmentsAsync(int? employeeId, CancellationToken ct)
    {
        const string sql = """
            SELECT aa.Id, aa.AssetId, a.AssetTag, aa.EmployeeId,
                   e.FirstName || ' ' || e.LastName EmployeeName,
                   aa.AssignedAtUtc, aa.AssignedBy, aa.ConditionAtAssignment, aa.ReturnedAtUtc,
                   aa.ReturnReason, aa.ConditionAtReturn, aa.Status, aa.EmployeeAcceptedAtUtc, aa.EmployeeSignatureName
            FROM AssetAssignments aa
            JOIN Assets a ON a.Id = aa.AssetId
            JOIN Employees e ON e.Id = aa.EmployeeId
            WHERE (@employeeId IS NULL OR aa.EmployeeId = @employeeId)
            ORDER BY aa.AssignedAtUtc DESC;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<AssignmentDto>(new CommandDefinition(sql, new { employeeId }, cancellationToken: ct))).AsList();
    }

    public async Task<AssignmentDto> AssignAssetAsync(AssignAssetDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var status = await db.ExecuteScalarAsync<string?>(new CommandDefinition("SELECT Status FROM Assets WHERE Id=@id", new { id = dto.AssetId }, tx, cancellationToken: ct));
        if (status is null) throw new InvalidOperationException("Asset not found.");
        if (!string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only available assets can be assigned.");

        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO AssetAssignments(AssetId,EmployeeId,AssignedAtUtc,AssignedBy,ConditionAtAssignment,Status)
            VALUES(@AssetId,@EmployeeId,@now,@actor,@Condition,'Assigned'); SELECT last_insert_rowid();
            """, new { dto.AssetId, dto.EmployeeId, dto.Condition, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status='Assigned', Condition=@Condition, UpdatedAtUtc=@now WHERE Id=@AssetId", new { dto.AssetId, dto.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@AssetId,'Assigned','Asset assigned to employee ' || @EmployeeId,@actor,@now)", new { dto.AssetId, dto.EmployeeId, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return (await GetAssignmentsAsync(dto.EmployeeId, ct)).Single(x => x.Id == id);
    }

    public async Task AcceptAssetAsync(int assignmentId, AcceptAssetDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(new CommandDefinition("UPDATE AssetAssignments SET EmployeeAcceptedAtUtc=@now, EmployeeSignatureName=@name WHERE Id=@id AND Status='Assigned'", new { id = assignmentId, now = DateTime.UtcNow, name = dto.SignatureName.Trim() }, cancellationToken: ct));
        if (affected == 0) throw new InvalidOperationException("Active assignment not found.");
    }

    public async Task ReturnAssetAsync(int assignmentId, ReturnAssetDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var assetId = await db.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT AssetId FROM AssetAssignments WHERE Id=@id AND Status='Assigned'", new { id = assignmentId }, tx, cancellationToken: ct));
        if (assetId is null) throw new InvalidOperationException("Active assignment not found.");
        var nextStatus = dto.Condition.Equals("Damaged", StringComparison.OrdinalIgnoreCase) ? "Pending Write-Off" : "Available";
        await db.ExecuteAsync(new CommandDefinition("UPDATE AssetAssignments SET ReturnedAtUtc=@now,ReturnedTo=@actor,ReturnReason=@Reason,ConditionAtReturn=@Condition,Status='Returned' WHERE Id=@id", new { id = assignmentId, actor, dto.Reason, dto.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status=@nextStatus,Condition=@Condition,Notes=COALESCE(@Notes,Notes),UpdatedAtUtc=@now WHERE Id=@assetId", new { assetId, nextStatus, dto.Condition, dto.Notes, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'Returned',@description,@actor,@now)", new { assetId, description = $"Returned: {dto.Reason}; condition: {dto.Condition}", actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task EnsureTasksAsync(int employeeId, string lifecycleType, CancellationToken ct)
    {
        var isOffboarding = lifecycleType.Equals("Offboarding", StringComparison.OrdinalIgnoreCase);
        var tasks = isOffboarding
            ? new[] { ("ASSETS_RETURNED", "All company assets returned"), ("EMAIL_DISABLED", "Email disabled"), ("SAP_DISABLED", "SAP account disabled"), ("VPN_DISABLED", "VPN access disabled"), ("CISTECH_DISABLED", "CisTech account disabled"), ("GROUPS_REMOVED", "Groups and shared-folder access removed") }
            : new[] { ("EMAIL_CREATED", "Email created"), ("SAP_CREATED", "SAP account created"), ("VPN_CONFIGURED", "VPN configured"), ("CISTECH_CREATED", "CisTech account created"), ("GROUPS_ADDED", "Relevant user/email groups added"), ("PRINT_SCAN", "Print/scan access configured"), ("SHARED_FOLDERS", "Shared folders configured"), ("PRINTERS", "Printers configured"), ("ASSETS_ASSIGNED", "Required equipment assigned"), ("ACCEPTANCE_SIGNED", "Employee acceptance signed") };
        await using var db = connectionFactory.CreateConnection();
        foreach (var task in tasks)
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO EmployeeLifecycleTasks(EmployeeId,LifecycleType,TaskCode,TaskName,IsCompleted)
                SELECT @employeeId,@lifecycleType,@code,@name,0
                WHERE NOT EXISTS(SELECT 1 FROM EmployeeLifecycleTasks WHERE EmployeeId=@employeeId AND LifecycleType=@lifecycleType AND TaskCode=@code);
                """, new { employeeId, lifecycleType = isOffboarding ? "Offboarding" : "Onboarding", code = task.Item1, name = task.Item2 }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<LifecycleTaskDto>> GetTasksAsync(int employeeId, string lifecycleType, CancellationToken ct)
    {
        await EnsureTasksAsync(employeeId, lifecycleType, ct);
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<LifecycleTaskDto>(new CommandDefinition("SELECT Id,EmployeeId,LifecycleType,TaskCode,TaskName,IsCompleted,CompletedBy,CompletedAtUtc,Notes FROM EmployeeLifecycleTasks WHERE EmployeeId=@employeeId AND LifecycleType=@lifecycleType ORDER BY Id", new { employeeId, lifecycleType }, cancellationToken: ct))).AsList();
    }

    public async Task UpdateTaskAsync(int taskId, UpdateLifecycleTaskDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.ExecuteAsync(new CommandDefinition("UPDATE EmployeeLifecycleTasks SET IsCompleted=@done,CompletedBy=CASE WHEN @done=1 THEN @actor ELSE NULL END,CompletedAtUtc=CASE WHEN @done=1 THEN @now ELSE NULL END,Notes=@notes WHERE Id=@id", new { id = taskId, done = dto.IsCompleted, actor, now = DateTime.UtcNow, notes = dto.Notes }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<WriteOffDto>> GetWriteOffsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT w.Id,w.AssetId,a.AssetTag,a.SerialNumber,a.Make,a.Model,a.Category,w.Condition,w.CurrentValue,a.FullyDepreciated,
                   w.Reason,w.RequestedBy,w.RequestedAtUtc,w.ApprovalStatus,w.ApprovedBy,w.ApprovedAtUtc,
                   w.DisposalMethod,w.DisposalCompany,w.DisposalDate,w.CertificateReference,w.Notes
            FROM WriteOffs w JOIN Assets a ON a.Id=w.AssetId ORDER BY w.RequestedAtUtc DESC;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<WriteOffDto>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<WriteOffDto> CreateWriteOffAsync(CreateWriteOffDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO WriteOffs(AssetId,Reason,Condition,CurrentValue,RequestedBy,RequestedAtUtc,ApprovalStatus,Notes)
            VALUES(@AssetId,@Reason,@Condition,@CurrentValue,@actor,@now,'Pending',@Notes); SELECT last_insert_rowid();
            """, new { dto.AssetId, dto.Reason, dto.Condition, dto.CurrentValue, dto.Notes, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status='Pending Write-Off',Condition=@Condition,UpdatedAtUtc=@now WHERE Id=@AssetId", new { dto.AssetId, dto.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@AssetId,'WriteOffRequested',@Reason,@actor,@now)", new { dto.AssetId, dto.Reason, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return (await GetWriteOffsAsync(ct)).Single(x => x.Id == id);
    }

    public async Task ApproveWriteOffAsync(int id, ApproveWriteOffDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var assetId = await db.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT AssetId FROM WriteOffs WHERE Id=@id", new { id }, tx, cancellationToken: ct));
        if (assetId is null) throw new InvalidOperationException("Write-off not found.");
        var status = dto.Approved ? "Approved" : "Rejected";
        await db.ExecuteAsync(new CommandDefinition("UPDATE WriteOffs SET ApprovalStatus=@status,ApprovedBy=@actor,ApprovedAtUtc=@now,Notes=COALESCE(@notes,Notes) WHERE Id=@id", new { id, status, actor, now = DateTime.UtcNow, notes = dto.Notes }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status=@assetStatus,UpdatedAtUtc=@now WHERE Id=@assetId", new { assetId, assetStatus = dto.Approved ? "Approved for Write-Off" : "Available", now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task DisposeWriteOffAsync(int id, DisposeWriteOffDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var assetId = await db.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT AssetId FROM WriteOffs WHERE Id=@id AND ApprovalStatus='Approved'", new { id }, tx, cancellationToken: ct));
        if (assetId is null) throw new InvalidOperationException("Only approved write-offs can be disposed.");
        await db.ExecuteAsync(new CommandDefinition("UPDATE WriteOffs SET DisposalMethod=@DisposalMethod,DisposalCompany=@DisposalCompany,DisposalDate=@DisposalDate,CertificateReference=@CertificateReference,Notes=COALESCE(@Notes,Notes) WHERE Id=@id", new { id, dto.DisposalMethod, dto.DisposalCompany, dto.DisposalDate, dto.CertificateReference, dto.Notes }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status='Disposed',UpdatedAtUtc=@now WHERE Id=@assetId", new { assetId, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'Disposed',@description,@actor,@now)", new { assetId, description = $"Disposed via {dto.DisposalMethod}", actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT
              (SELECT COUNT(*) FROM Assets) TotalAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='Assigned') AssignedAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='Available') AvailableAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='In Repair') InRepairAssets,
              (SELECT COUNT(*) FROM WriteOffs WHERE ApprovalStatus='Pending') PendingWriteOffs,
              COALESCE((SELECT SUM(PurchaseValue) FROM Assets),0) TotalAssetValue,
              COALESCE((SELECT SUM(PurchaseValue) FROM Assets WHERE Status='Available'),0) AvailableAssetValue,
              (SELECT COUNT(*) FROM AssetRequests WHERE Status NOT IN ('Completed','Cancelled','Rejected')) PendingRequests,
              (SELECT COUNT(*) FROM Employees WHERE EmploymentStatus='Active') ActiveEmployees,
              COALESCE((SELECT SUM(PurchaseValue) FROM Assets WHERE Status='Available'),0) EstimatedReuseValue;
            """;
        await using var db = connectionFactory.CreateConnection();
        return await db.QuerySingleAsync<DashboardDto>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task UpdateRequestWorkflowAsync(int id, UpdateRequestWorkflowDto dto, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests SET Status=@Status,
              ManagerApprovalStatus=COALESCE(@ManagerApprovalStatus,ManagerApprovalStatus),
              ITStatus=COALESCE(@ITStatus,ITStatus),
              ApprovedAtUtc=CASE WHEN @Status='Approved' AND ApprovedAtUtc IS NULL THEN @now ELSE ApprovedAtUtc END,
              CompletedAtUtc=CASE WHEN @Status='Completed' THEN @now ELSE CompletedAtUtc END
            WHERE Id=@id;
            """, new { id, dto.Status, dto.ManagerApprovalStatus, dto.ITStatus, now = DateTime.UtcNow }, cancellationToken: ct));
    }

    public async Task<AssignmentDto?> GetAssignmentAsync(int id, CancellationToken ct)
    {
        var all = await GetAssignmentsAsync(null, ct);
        return all.SingleOrDefault(x => x.Id == id);
    }

    public async Task<ImportResultDto> ImportEwasteAsync(IReadOnlyList<ImportEwasteRowDto> rows, string actor, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;
        var warnings = new List<string>();
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SerialNumber)) { skipped++; warnings.Add("Skipped row with no serial number."); continue; }
            var existing = await db.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT Id FROM Assets WHERE SerialNumber=@serial", new { serial = row.SerialNumber }, cancellationToken: ct));
            if (existing is not null) { skipped++; warnings.Add($"Serial {row.SerialNumber} already exists."); continue; }
            await using var tx = await db.BeginTransactionAsync(ct);
            var tag = $"EW-{row.SerialNumber}";
            var assetId = await db.ExecuteScalarAsync<int>(new CommandDefinition("""
                INSERT INTO Assets(AssetTag,SerialNumber,Category,Make,Model,Status,Condition,PurchaseValue,FullyDepreciated,Notes,CreatedAtUtc,UpdatedAtUtc)
                VALUES(@tag,@SerialNumber,@Category,@Make,@Model,@status,@Condition,@Value,@FullyDepreciated,@Notes,@now,@now); SELECT last_insert_rowid();
                """, new { tag, row.SerialNumber, row.Category, row.Make, row.Model, status = row.DisposalDate is null ? "Approved for Write-Off" : "Disposed", row.Condition, row.Value, row.FullyDepreciated, row.Notes, now = DateTime.UtcNow }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO WriteOffs(AssetId,Reason,Condition,CurrentValue,RequestedBy,RequestedAtUtc,ApprovalStatus,ApprovedBy,ApprovedAtUtc,DisposalMethod,DisposalDate,Notes)
                VALUES(@assetId,'Imported legacy e-waste record',@Condition,@Value,@actor,@now,'Approved',@ApprovedBy,@now,@DisposalMethod,@DisposalDate,@Notes);
                """, new { assetId, row.Condition, row.Value, actor, now = DateTime.UtcNow, row.ApprovedBy, row.DisposalMethod, row.DisposalDate, row.Notes }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            imported++;
        }
        return new ImportResultDto(imported, skipped, warnings);
    }
}
