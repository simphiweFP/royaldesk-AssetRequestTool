using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.Lifecycle;

internal sealed class LifecycleRepository(IDbConnectionFactory connectionFactory) : ILifecycleRepository
{
    public async Task<IReadOnlyList<LookupRecord>> GetBranchesAsync(CancellationToken ct)
    {
        const string sql = "SELECT Id, CAST(Code AS TEXT) Code, CAST(Name AS TEXT) Name FROM Branches WHERE IsActive = 1 ORDER BY Name";
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<LookupRecord>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<DepartmentRecord>> GetDepartmentsAsync(CancellationToken ct)
    {
        const string sql = "SELECT Id, CAST(Name AS TEXT) Name FROM Departments WHERE IsActive = 1 ORDER BY Name";
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<DepartmentRecord>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<EmployeeRecord>> GetEmployeesAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT e.Id,
                   CAST(e.EmployeeNumber AS TEXT) EmployeeNumber,
                   CAST(e.FirstName AS TEXT) FirstName,
                   CAST(e.LastName AS TEXT) LastName,
                   CAST(e.Email AS TEXT) Email,
                   CAST(e.Username AS TEXT) Username,
                   CAST(e.JobTitle AS TEXT) JobTitle,
                   e.BranchId,
                   CAST(b.Name AS TEXT) BranchName,
                   e.DepartmentId,
                   CAST(d.Name AS TEXT) DepartmentName,
                   CAST(e.EmploymentStatus AS TEXT) EmploymentStatus,
                   CAST(e.StartDate AS TEXT) StartDate,
                   CAST(e.EndDate AS TEXT) EndDate
            FROM Employees e
            LEFT JOIN Branches b ON b.Id = e.BranchId
            LEFT JOIN Departments d ON d.Id = e.DepartmentId
            ORDER BY e.LastName, e.FirstName;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<EmployeeRecord>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<EmployeeRecord> CreateEmployeeAsync(CreateEmployeeCommand command, CancellationToken ct)
    {
        const string insert = """
            INSERT INTO Employees(EmployeeNumber, FirstName, LastName, Email, Username, JobTitle, BranchId, DepartmentId, EmploymentStatus, StartDate, CreatedAtUtc, UpdatedAtUtc)
            VALUES(@EmployeeNumber,@FirstName,@LastName,@Email,@Username,@JobTitle,@BranchId,@DepartmentId,'Active',@StartDate,@Now,@Now);
            SELECT last_insert_rowid();
            """;

        await using var db = connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<long>(new CommandDefinition(insert, new
        {
            command.EmployeeNumber,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Username,
            command.JobTitle,
            command.BranchId,
            command.DepartmentId,
            command.StartDate,
            Now = DateTime.UtcNow
        }, cancellationToken: ct));

        var employees = await GetEmployeesAsync(ct);
        return employees.Single(x => x.Id == id);
    }

    public async Task<IReadOnlyList<AssetRecord>> GetAssetsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT a.Id,
                   CAST(a.AssetTag AS TEXT) AssetTag,
                   CAST(a.SerialNumber AS TEXT) SerialNumber,
                   CAST(a.Category AS TEXT) Category,
                   CAST(a.Make AS TEXT) Make,
                   CAST(a.Model AS TEXT) Model,
                   a.BranchId,
                   CAST(b.Name AS TEXT) BranchName,
                   a.DepartmentId,
                   CAST(d.Name AS TEXT) DepartmentName,
                   CAST(a.Status AS TEXT) Status,
                   CAST(a.Condition AS TEXT) Condition,
                   CAST(a.PurchaseDate AS TEXT) PurchaseDate,
                   CAST(a.PurchaseValue AS TEXT) PurchaseValue,
                   CAST(a.WarrantyExpiry AS TEXT) WarrantyExpiry,
                   CAST(COALESCE(a.FullyDepreciated,0) AS INTEGER) FullyDepreciated,
                   CAST(a.Notes AS TEXT) Notes,
                   aa.EmployeeId AssignedEmployeeId,
                   CASE WHEN e.Id IS NULL THEN NULL ELSE CAST(e.FirstName || ' ' || e.LastName AS TEXT) END AssignedEmployeeName
            FROM Assets a
            LEFT JOIN Branches b ON b.Id = a.BranchId
            LEFT JOIN Departments d ON d.Id = a.DepartmentId
            LEFT JOIN AssetAssignments aa ON aa.AssetId = a.Id AND aa.Status = 'Assigned'
            LEFT JOIN Employees e ON e.Id = aa.EmployeeId
            ORDER BY a.AssetTag;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<AssetRecord>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<AssetRecord> CreateAssetAsync(CreateAssetCommand command, string actor, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Assets(AssetTag,SerialNumber,Category,Make,Model,BranchId,DepartmentId,Status,Condition,PurchaseDate,PurchaseValue,WarrantyExpiry,FullyDepreciated,Notes,CreatedAtUtc,UpdatedAtUtc)
            VALUES(@AssetTag,@SerialNumber,@Category,@Make,@Model,@BranchId,@DepartmentId,'Available',@Condition,@PurchaseDate,@PurchaseValue,@WarrantyExpiry,@FullyDepreciated,@Notes,@Now,@Now);
            SELECT last_insert_rowid();
            """;

        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var id = await db.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            command.AssetTag,
            command.SerialNumber,
            command.Category,
            command.Make,
            command.Model,
            command.BranchId,
            command.DepartmentId,
            command.Condition,
            command.PurchaseDate,
            command.PurchaseValue,
            command.WarrantyExpiry,
            command.FullyDepreciated,
            command.Notes,
            Now = DateTime.UtcNow
        }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@id,'Created','Asset registered',@actor,@now)",
            new { id, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return (await GetAssetsAsync(ct)).Single(x => x.Id == id);
    }

    public async Task<IReadOnlyList<AssignmentRecord>> GetAssignmentsAsync(int? employeeId, CancellationToken ct)
    {
        const string sql = """
            SELECT aa.Id,
                   aa.AssetId,
                   CAST(a.AssetTag AS TEXT) AssetTag,
                   aa.EmployeeId,
                   CAST(e.FirstName || ' ' || e.LastName AS TEXT) EmployeeName,
                   CAST(aa.AssignedAtUtc AS TEXT) AssignedAtUtc,
                   CAST(aa.AssignedBy AS TEXT) AssignedBy,
                   CAST(aa.ConditionAtAssignment AS TEXT) ConditionAtAssignment,
                   CAST(aa.ReturnedAtUtc AS TEXT) ReturnedAtUtc,
                   CAST(aa.ReturnReason AS TEXT) ReturnReason,
                   CAST(aa.ConditionAtReturn AS TEXT) ConditionAtReturn,
                   CAST(aa.Status AS TEXT) Status,
                   CAST(aa.EmployeeAcceptedAtUtc AS TEXT) EmployeeAcceptedAtUtc,
                   CAST(aa.EmployeeSignatureName AS TEXT) EmployeeSignatureName
            FROM AssetAssignments aa
            JOIN Assets a ON a.Id = aa.AssetId
            JOIN Employees e ON e.Id = aa.EmployeeId
            WHERE (@employeeId IS NULL OR aa.EmployeeId = @employeeId)
            ORDER BY aa.AssignedAtUtc DESC;
            """;
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<AssignmentRecord>(new CommandDefinition(sql, new { employeeId }, cancellationToken: ct))).AsList();
    }

    public async Task<AssignmentRecord> AssignAssetAsync(AssignAssetCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var status = await db.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT CAST(Status AS TEXT) FROM Assets WHERE Id=@id",
            new { id = command.AssetId }, tx, cancellationToken: ct));

        if (status is null) throw new InvalidOperationException("Asset not found.");
        if (!string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only available assets can be assigned.");

        var id = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
            INSERT INTO AssetAssignments(AssetId,EmployeeId,AssignedAtUtc,AssignedBy,ConditionAtAssignment,Status)
            VALUES(@AssetId,@EmployeeId,@now,@actor,@Condition,'Assigned');
            SELECT last_insert_rowid();
            """, new { command.AssetId, command.EmployeeId, command.Condition, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status='Assigned', Condition=@Condition, UpdatedAtUtc=@now WHERE Id=@AssetId",
            new { command.AssetId, command.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@AssetId,'Assigned','Asset assigned to employee ' || @EmployeeId,@actor,@now)",
            new { command.AssetId, command.EmployeeId, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return (await GetAssignmentsAsync(command.EmployeeId, ct)).Single(x => x.Id == id);
    }

    public async Task AcceptAssetAsync(int assignmentId, AcceptAssetCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        var affected = await db.ExecuteAsync(new CommandDefinition(
            "UPDATE AssetAssignments SET EmployeeAcceptedAtUtc=@now, EmployeeSignatureName=@name WHERE Id=@id AND Status='Assigned'",
            new { id = assignmentId, now = DateTime.UtcNow, name = command.SignatureName.Trim() }, cancellationToken: ct));

        if (affected == 0) throw new InvalidOperationException("Active assignment not found.");
    }

    public async Task ReturnAssetAsync(int assignmentId, ReturnAssetCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var assetId = await db.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT AssetId FROM AssetAssignments WHERE Id=@id AND Status='Assigned'",
            new { id = assignmentId }, tx, cancellationToken: ct));

        if (assetId is null) throw new InvalidOperationException("Active assignment not found.");

        var nextStatus = command.Condition.Equals("Damaged", StringComparison.OrdinalIgnoreCase)
            ? "Pending Write-Off"
            : "Available";

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE AssetAssignments SET ReturnedAtUtc=@now,ReturnedTo=@actor,ReturnReason=@Reason,ConditionAtReturn=@Condition,Status='Returned' WHERE Id=@id",
            new { id = assignmentId, actor, command.Reason, command.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status=@nextStatus,Condition=@Condition,Notes=COALESCE(@Notes,Notes),UpdatedAtUtc=@now WHERE Id=@assetId",
            new { assetId, nextStatus, command.Condition, command.Notes, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'Returned',@description,@actor,@now)",
            new { assetId, description = $"Returned: {command.Reason}; condition: {command.Condition}", actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task EnsureTasksAsync(int employeeId, string lifecycleType, CancellationToken ct)
    {
        var isOffboarding = lifecycleType.Equals("Offboarding", StringComparison.OrdinalIgnoreCase);
        var tasks = isOffboarding
            ? new[]
            {
                ("ASSETS_RETURNED", "All company assets returned"),
                ("EMAIL_DISABLED", "Email disabled"),
                ("SAP_DISABLED", "SAP account disabled"),
                ("VPN_DISABLED", "VPN access disabled"),
                ("CISTECH_DISABLED", "CisTech account disabled"),
                ("GROUPS_REMOVED", "Groups and shared-folder access removed")
            }
            : new[]
            {
                ("EMAIL_CREATED", "Email created"),
                ("SAP_CREATED", "SAP account created"),
                ("VPN_CONFIGURED", "VPN configured"),
                ("CISTECH_CREATED", "CisTech account created"),
                ("GROUPS_ADDED", "Relevant user/email groups added"),
                ("PRINT_SCAN", "Print/scan access configured"),
                ("SHARED_FOLDERS", "Shared folders configured"),
                ("PRINTERS", "Printers configured"),
                ("ASSETS_ASSIGNED", "Required equipment assigned"),
                ("ACCEPTANCE_SIGNED", "Employee acceptance signed")
            };

        await using var db = connectionFactory.CreateConnection();
        foreach (var task in tasks)
        {
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO EmployeeLifecycleTasks(EmployeeId,LifecycleType,TaskCode,TaskName,IsCompleted)
                SELECT @employeeId,@lifecycleType,@code,@name,0
                WHERE NOT EXISTS(
                    SELECT 1
                    FROM EmployeeLifecycleTasks
                    WHERE EmployeeId=@employeeId AND LifecycleType=@lifecycleType AND TaskCode=@code
                );
                """, new
            {
                employeeId,
                lifecycleType = isOffboarding ? "Offboarding" : "Onboarding",
                code = task.Item1,
                name = task.Item2
            }, cancellationToken: ct));
        }
    }

    public async Task<IReadOnlyList<LifecycleTaskRecord>> GetTasksAsync(int employeeId, string lifecycleType, CancellationToken ct)
    {
        await EnsureTasksAsync(employeeId, lifecycleType, ct);

        const string sql = """
            SELECT Id,
                   EmployeeId,
                   CAST(LifecycleType AS TEXT) LifecycleType,
                   CAST(TaskCode AS TEXT) TaskCode,
                   CAST(TaskName AS TEXT) TaskName,
                   CAST(COALESCE(IsCompleted,0) AS INTEGER) IsCompleted,
                   CAST(CompletedBy AS TEXT) CompletedBy,
                   CAST(CompletedAtUtc AS TEXT) CompletedAtUtc,
                   CAST(Notes AS TEXT) Notes
            FROM EmployeeLifecycleTasks
            WHERE EmployeeId=@employeeId AND LifecycleType=@lifecycleType
            ORDER BY Id;
            """;

        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<LifecycleTaskRecord>(new CommandDefinition(sql, new { employeeId, lifecycleType }, cancellationToken: ct))).AsList();
    }

    public async Task UpdateTaskAsync(int taskId, UpdateLifecycleTaskCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE EmployeeLifecycleTasks
            SET IsCompleted=@done,
                CompletedBy=CASE WHEN @done=1 THEN @actor ELSE NULL END,
                CompletedAtUtc=CASE WHEN @done=1 THEN @now ELSE NULL END,
                Notes=@notes
            WHERE Id=@id;
            """, new
        {
            id = taskId,
            done = command.IsCompleted,
            actor,
            now = DateTime.UtcNow,
            notes = command.Notes
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<WriteOffRecord>> GetWriteOffsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT w.Id,
                   w.AssetId,
                   CAST(a.AssetTag AS TEXT) AssetTag,
                   CAST(a.SerialNumber AS TEXT) SerialNumber,
                   CAST(a.Make AS TEXT) Make,
                   CAST(a.Model AS TEXT) Model,
                   CAST(a.Category AS TEXT) Category,
                   CAST(w.Condition AS TEXT) Condition,
                   CAST(w.CurrentValue AS TEXT) CurrentValue,
                   CAST(COALESCE(a.FullyDepreciated,0) AS INTEGER) FullyDepreciated,
                   CAST(w.Reason AS TEXT) Reason,
                   CAST(w.RequestedBy AS TEXT) RequestedBy,
                   CAST(w.RequestedAtUtc AS TEXT) RequestedAtUtc,
                   CAST(w.ApprovalStatus AS TEXT) ApprovalStatus,
                   CAST(w.ApprovedBy AS TEXT) ApprovedBy,
                   CAST(w.ApprovedAtUtc AS TEXT) ApprovedAtUtc,
                   CAST(w.DisposalMethod AS TEXT) DisposalMethod,
                   CAST(w.DisposalCompany AS TEXT) DisposalCompany,
                   CAST(w.DisposalDate AS TEXT) DisposalDate,
                   CAST(w.CertificateReference AS TEXT) CertificateReference,
                   CAST(w.Notes AS TEXT) Notes
            FROM WriteOffs w
            JOIN Assets a ON a.Id=w.AssetId
            ORDER BY w.RequestedAtUtc DESC;
            """;

        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<WriteOffRecord>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<WriteOffRecord> CreateWriteOffAsync(CreateWriteOffCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var id = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
            INSERT INTO WriteOffs(AssetId,Reason,Condition,CurrentValue,RequestedBy,RequestedAtUtc,ApprovalStatus,Notes)
            VALUES(@AssetId,@Reason,@Condition,@CurrentValue,@actor,@now,'Pending',@Notes);
            SELECT last_insert_rowid();
            """, new
        {
            command.AssetId,
            command.Reason,
            command.Condition,
            command.CurrentValue,
            command.Notes,
            actor,
            now = DateTime.UtcNow
        }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status='Pending Write-Off',Condition=@Condition,UpdatedAtUtc=@now WHERE Id=@AssetId",
            new { command.AssetId, command.Condition, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@AssetId,'WriteOffRequested',@Reason,@actor,@now)",
            new { command.AssetId, command.Reason, actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return (await GetWriteOffsAsync(ct)).Single(x => x.Id == id);
    }

    public async Task ApproveWriteOffAsync(int id, ApproveWriteOffCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var assetId = await db.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT AssetId FROM WriteOffs WHERE Id=@id", new { id }, tx, cancellationToken: ct));

        if (assetId is null) throw new InvalidOperationException("Write-off not found.");

        var status = command.Approved ? "Approved" : "Rejected";
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE WriteOffs SET ApprovalStatus=@status,ApprovedBy=@actor,ApprovedAtUtc=@now,Notes=COALESCE(@notes,Notes) WHERE Id=@id",
            new { id, status, actor, now = DateTime.UtcNow, notes = command.Notes }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status=@assetStatus,UpdatedAtUtc=@now WHERE Id=@assetId",
            new { assetId, assetStatus = command.Approved ? "Approved for Write-Off" : "Available", now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task DisposeWriteOffAsync(int id, DisposeWriteOffCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);

        var assetId = await db.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT AssetId FROM WriteOffs WHERE Id=@id AND ApprovalStatus='Approved'",
            new { id }, tx, cancellationToken: ct));

        if (assetId is null) throw new InvalidOperationException("Only approved write-offs can be disposed.");

        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE WriteOffs
            SET DisposalMethod=@DisposalMethod,
                DisposalCompany=@DisposalCompany,
                DisposalDate=@DisposalDate,
                CertificateReference=@CertificateReference,
                Notes=COALESCE(@Notes,Notes)
            WHERE Id=@id;
            """, new
        {
            id,
            command.DisposalMethod,
            command.DisposalCompany,
            command.DisposalDate,
            command.CertificateReference,
            command.Notes
        }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status='Disposed',UpdatedAtUtc=@now WHERE Id=@assetId",
            new { assetId, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'Disposed',@description,@actor,@now)",
            new { assetId, description = $"Disposed via {command.DisposalMethod}", actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task<DashboardRecord> GetDashboardAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT
              (SELECT COUNT(*) FROM Assets) TotalAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='Assigned') AssignedAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='Available') AvailableAssets,
              (SELECT COUNT(*) FROM Assets WHERE Status='In Repair') InRepairAssets,
              (SELECT COUNT(*) FROM WriteOffs WHERE ApprovalStatus='Pending') PendingWriteOffs,
              CAST(COALESCE((SELECT SUM(CAST(PurchaseValue AS NUMERIC)) FROM Assets),0) AS TEXT) TotalAssetValue,
              CAST(COALESCE((SELECT SUM(CAST(PurchaseValue AS NUMERIC)) FROM Assets WHERE Status='Available'),0) AS TEXT) AvailableAssetValue,
              (SELECT COUNT(*) FROM AssetRequests WHERE Status NOT IN ('Completed','Cancelled','Rejected')) PendingRequests,
              (SELECT COUNT(*) FROM Employees WHERE EmploymentStatus='Active') ActiveEmployees,
              CAST(COALESCE((SELECT SUM(CAST(PurchaseValue AS NUMERIC)) FROM Assets WHERE Status='Available'),0) AS TEXT) EstimatedReuseValue;
            """;

        await using var db = connectionFactory.CreateConnection();
        return await db.QuerySingleAsync<DashboardRecord>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task UpdateRequestWorkflowAsync(int id, UpdateRequestWorkflowCommand command, string actor, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests
            SET Status=@Status,
                ManagerApprovalStatus=COALESCE(@ManagerApprovalStatus,ManagerApprovalStatus),
                ITStatus=COALESCE(@ITStatus,ITStatus),
                ApprovedAtUtc=CASE WHEN @Status='Approved' AND ApprovedAtUtc IS NULL THEN @now ELSE ApprovedAtUtc END,
                CompletedAtUtc=CASE WHEN @Status='Completed' THEN @now ELSE CompletedAtUtc END
            WHERE Id=@id;
            """, new
        {
            id,
            command.Status,
            command.ManagerApprovalStatus,
            command.ITStatus,
            now = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task<AssignmentRecord?> GetAssignmentAsync(int id, CancellationToken ct)
    {
        var all = await GetAssignmentsAsync(null, ct);
        return all.SingleOrDefault(x => x.Id == id);
    }

    public async Task<ImportResultRecord> ImportEwasteAsync(IReadOnlyList<ImportEwasteRowCommand> rows, string actor, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;
        var warnings = new List<string>();

        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                skipped++;
                warnings.Add("Skipped row with no serial number.");
                continue;
            }

            var existing = await db.ExecuteScalarAsync<long?>(new CommandDefinition(
                "SELECT Id FROM Assets WHERE SerialNumber=@serial",
                new { serial = row.SerialNumber }, cancellationToken: ct));

            if (existing is not null)
            {
                skipped++;
                warnings.Add($"Serial {row.SerialNumber} already exists.");
                continue;
            }

            await using var tx = await db.BeginTransactionAsync(ct);
            var tag = $"EW-{row.SerialNumber}";

            var assetId = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO Assets(AssetTag,SerialNumber,Category,Make,Model,Status,Condition,PurchaseValue,FullyDepreciated,Notes,CreatedAtUtc,UpdatedAtUtc)
                VALUES(@tag,@SerialNumber,@Category,@Make,@Model,@status,@Condition,@Value,@FullyDepreciated,@Notes,@now,@now);
                SELECT last_insert_rowid();
                """, new
            {
                tag,
                row.SerialNumber,
                row.Category,
                row.Make,
                row.Model,
                status = row.DisposalDate is null ? "Approved for Write-Off" : "Disposed",
                row.Condition,
                row.Value,
                row.FullyDepreciated,
                row.Notes,
                now = DateTime.UtcNow
            }, tx, cancellationToken: ct));

            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO WriteOffs(AssetId,Reason,Condition,CurrentValue,RequestedBy,RequestedAtUtc,ApprovalStatus,ApprovedBy,ApprovedAtUtc,DisposalMethod,DisposalDate,Notes)
                VALUES(@assetId,'Imported legacy e-waste record',@Condition,@Value,@actor,@now,'Approved',@ApprovedBy,@now,@DisposalMethod,@DisposalDate,@Notes);
                """, new
            {
                assetId,
                row.Condition,
                row.Value,
                actor,
                now = DateTime.UtcNow,
                row.ApprovedBy,
                row.DisposalMethod,
                row.DisposalDate,
                row.Notes
            }, tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            imported++;
        }

        return new ImportResultRecord(imported, skipped, warnings);
    }
}
