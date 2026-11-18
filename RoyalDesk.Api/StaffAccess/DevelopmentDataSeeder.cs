using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.StaffAccess;

/// <summary>
/// Optional local-development demo records. Additive and idempotent, no emails are sent.
/// Uses an explicit enable flag, refuses a non-development environment, and requires
/// an empty, dedicated demo database for its first invocation.
/// </summary>
public sealed class DevelopmentDataSeeder(IDbConnectionFactory connections, IWebHostEnvironment environment, IConfiguration configuration)
{
    private const string Marker = "royaldesk-full-demo-v1";
    private const string Actor = "ROYALDESK-DEMO-SEED";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!string.Equals(configuration["ROYALDESK_SEED_FULL_DEMO"], "true", StringComparison.OrdinalIgnoreCase))
            return;
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Full demo data can only be seeded in Development.");

        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);

        // Never silently populate a database containing real business records.
        await db.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS RoyalDeskDemoSeedRuns (
                SeedKey TEXT PRIMARY KEY NOT NULL,
                SeededAtUtc TEXT NOT NULL
            );
            """, cancellationToken: ct));
        if (await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM RoyalDeskDemoSeedRuns WHERE SeedKey=@key", new { key = Marker }, cancellationToken: ct)) > 0)
            return;

        foreach (var table in new[] { "Employees", "Assets", "AssetRequests", "StoreItems", "WriteOffs" })
        {
            var count = await db.ExecuteScalarAsync<int>(new CommandDefinition(
                $"SELECT COUNT(*) FROM {table}", cancellationToken: ct));
            if (count != 0)
                throw new InvalidOperationException(
                    $"Full demo seeding requires a fresh dedicated development database; {table} already contains records.");
        }

        await using var tx = await db.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var branch = await db.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT Id FROM Branches WHERE IsActive=1 ORDER BY Id LIMIT 1", transaction: tx, cancellationToken: ct));
        var department = await db.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT Id FROM Departments WHERE IsActive=1 ORDER BY Id LIMIT 1", transaction: tx, cancellationToken: ct));
        if (!branch.HasValue || !department.HasValue)
            throw new InvalidOperationException("Run the standard branch/department migrations and lookup seeds before full demo seeding.");

        var staff = new[]
        {
            (Number:"SIM-EMP-001", First:"Anele", Last:"Mokoena", Email:"anele.mokoena@example.test", Job:"Accounts Clerk"),
            (Number:"SIM-EMP-002", First:"Thando", Last:"Naidoo", Email:"thando.naidoo@example.test", Job:"Sales Representative"),
            (Number:"SIM-EMP-003", First:"Lerato", Last:"Dube", Email:"lerato.dube@example.test", Job:"Operations Coordinator"),
            (Number:"SIM-EMP-004", First:"Musa", Last:"Khumalo", Email:"musa.khumalo@example.test", Job:"Store Room Assistant"),
            (Number:"SIM-EMP-005", First:"Zanele", Last:"Pillay", Email:"zanele.pillay@example.test", Job:"Finance Officer"),
            (Number:"SIM-EMP-006", First:"Siyabonga", Last:"Cele", Email:"siyabonga.cele@example.test", Job:"IT Support")
        };
        var employeeIds = new List<long>();
        foreach (var employee in staff)
        {
            employeeIds.Add(await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO Employees(EmployeeNumber,FirstName,LastName,Email,Username,JobTitle,BranchId,DepartmentId,EmploymentStatus,StartDate,CreatedAtUtc,UpdatedAtUtc)
                VALUES(@Number,@First,@Last,@Email,@Email,@Job,@branch,@department,'Active',@start,@now,@now);
                SELECT last_insert_rowid();
                """, new { employee.Number, employee.First, employee.Last, employee.Email, employee.Job, branch,
                    department, start = now.AddMonths(-4).ToString("yyyy-MM-dd"), now }, tx, cancellationToken: ct)));
        }

        var assets = new[]
        {
            (Tag:"SIM-LAP-001", Category:"Laptop", Make:"Dell", Model:"Latitude 5440", Condition:"Good", Value:19500m),
            (Tag:"SIM-LAP-002", Category:"Laptop", Make:"Lenovo", Model:"ThinkPad E14", Condition:"Good", Value:16900m),
            (Tag:"SIM-LAP-003", Category:"Laptop", Make:"HP", Model:"ProBook 450", Condition:"Good", Value:15500m),
            (Tag:"SIM-DES-001", Category:"Desktop", Make:"Dell", Model:"OptiPlex", Condition:"Good", Value:11900m),
            (Tag:"SIM-MON-001", Category:"Monitor", Make:"Samsung", Model:"24 inch", Condition:"Good", Value:3800m),
            (Tag:"SIM-MON-002", Category:"Monitor", Make:"Dell", Model:"P2422H", Condition:"Good", Value:3600m),
            (Tag:"SIM-PRN-001", Category:"Printer", Make:"HP", Model:"LaserJet Pro", Condition:"Good", Value:7800m),
            (Tag:"SIM-LAP-004", Category:"Laptop", Make:"Lenovo", Model:"ThinkPad T14", Condition:"Damaged", Value:12000m)
        };
        var assetIds = new List<long>();
        foreach (var asset in assets)
        {
            var id = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO Assets(AssetTag,SerialNumber,Category,Make,Model,BranchId,DepartmentId,Status,Condition,
                                   PurchaseDate,PurchaseValue,FullyDepreciated,Notes,CreatedAtUtc,UpdatedAtUtc)
                VALUES(@Tag,@serial,@Category,@Make,@Model,@branch,@department,@status,@Condition,
                       @purchased,@Value,0,@notes,@now,@now);
                SELECT last_insert_rowid();
                """, new { asset.Tag, serial = "DEMO-" + asset.Tag, asset.Category, asset.Make, asset.Model,
                    branch, department, asset.Condition, asset.Value, status = asset.Condition == "Damaged" ? "In Repair" : "Available",
                    purchased = now.AddMonths(-10).ToString("yyyy-MM-dd"),
                    notes = "DEMO ONLY - fabricated asset; not company property", now }, tx, cancellationToken: ct));
            assetIds.Add(id);
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc)
                VALUES(@id,'Created','Development demo asset created',@actor,@now)
                """, new { id, actor = Actor, now }, tx, cancellationToken: ct));
        }

        var stock = new[]
        {
            (Name:"Laptop - Dell Latitude", Category:"Computer", Sku:"SIM-STK-LAP", Qty:4, Minimum:1, Tracked:true),
            (Name:"USB Keyboard", Category:"Peripheral", Sku:"SIM-STK-KBD", Qty:18, Minimum:5, Tracked:false),
            (Name:"Wireless Mouse", Category:"Peripheral", Sku:"SIM-STK-MSE", Qty:20, Minimum:5, Tracked:false),
            (Name:"24 inch Monitor", Category:"Display", Sku:"SIM-STK-MON", Qty:5, Minimum:2, Tracked:true),
            (Name:"USB-C Docking Station", Category:"Accessory", Sku:"SIM-STK-DOC", Qty:6, Minimum:2, Tracked:false)
        };
        foreach (var item in stock)
        {
            var stockId = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO StoreItems(Name,Category,Sku,Location,QuantityOnHand,ReservedQuantity,MinimumStock,IsAssetTracked,IsActive,CreatedAtUtc)
                VALUES(@Name,@Category,@Sku,'DEMO Store Room',@Qty,0,@Minimum,@Tracked,1,@now);
                SELECT last_insert_rowid();
                """, new { item.Name, item.Category, item.Sku, item.Qty, item.Minimum, item.Tracked, now }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO StoreMovements(StoreItemId,TicketId,MovementType,Quantity,Reference,Notes,PerformedBy,PerformedAtUtc)
                VALUES(@stockId,NULL,'Stock In',@quantity,'DEMO OPENING','Fabricated development stock',@actor,@now)
                """, new { stockId, quantity = item.Qty, actor = Actor, now }, tx, cancellationToken: ct));
        }

        // Assigned and accepted, available, damaged and write-off states.
        for (var i = 0; i < 2; i++)
        {
            var assignment = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO AssetAssignments(AssetId,EmployeeId,AssignedAtUtc,AssignedBy,ConditionAtAssignment,
                                             Status,EmployeeAcceptedAtUtc,EmployeeSignatureName)
                VALUES(@asset,@employee,@assigned,@actor,'Good','Assigned',@accepted,@name);
                SELECT last_insert_rowid();
                """, new { asset = assetIds[i], employee = employeeIds[i], assigned = now.AddDays(-30),
                    actor = Actor, accepted = now.AddDays(-29), name = staff[i].First + " " + staff[i].Last }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition(
                "UPDATE Assets SET Status='Assigned',UpdatedAtUtc=@now WHERE Id=@id",
                new { id = assetIds[i], now }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc)
                VALUES(@id,'Assigned','Demo employee accepted equipment',@actor,@now)
                """, new { id = assetIds[i], actor = Actor, now }, tx, cancellationToken: ct));
        }

        // Onboarding and offboarding examples (no real identity account changes).
        var tasks = new[]
        {
            (Index:2, Type:"Onboarding", Code:"EMAIL_CREATED", Name:"Email created", Done:true),
            (Index:2, Type:"Onboarding", Code:"ASSETS_ASSIGNED", Name:"Required equipment assigned", Done:false),
            (Index:4, Type:"Offboarding", Code:"ASSETS_RETURNED", Name:"All company assets returned", Done:false),
            (Index:4, Type:"Offboarding", Code:"EMAIL_DISABLED", Name:"Email disabled", Done:false)
        };
        foreach (var task in tasks)
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO EmployeeLifecycleTasks(EmployeeId,LifecycleType,TaskCode,TaskName,IsCompleted,CompletedBy,CompletedAtUtc,Notes)
                VALUES(@employee,@type,@code,@name,@done,@completedBy,@completedAt,'DEMO only; no real account changes')
                """, new { employee = employeeIds[task.Index], type = task.Type, code = task.Code,
                    name = task.Name, done = task.Done ? 1 : 0,
                    completedBy = task.Done ? Actor : null, completedAt = task.Done ? (DateTime?)now : null }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO WriteOffs(AssetId,Reason,Condition,CurrentValue,RequestedBy,RequestedAtUtc,ApprovalStatus,Notes)
            VALUES(@asset,'Beyond economical repair','Damaged',1200,@actor,@now,'Pending','DEMO test write-off; do not dispose real hardware')
            """, new { asset = assetIds[7], actor = Actor, now }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE Assets SET Status='Pending Write-Off' WHERE Id=@id",
            new { id = assetIds[7] }, tx, cancellationToken: ct));

        var scenarios = new[]
        {
            (Type:"Incident", Name:"Anele Mokoena", Email:"anele.mokoena@example.test", Category:"Email", Detail:"Outlook opens but mailbox does not synchronise.", Status:"New", Days:-1),
            (Type:"Incident", Name:"Anele Mokoena", Email:"anele.mokoena@example.test", Category:"Email", Detail:"Duplicate email synchronisation issue, merge with previous ticket.", Status:"New", Days:-1),
            (Type:"Incident", Name:"Thando Naidoo", Email:"thando.naidoo@example.test", Category:"Network", Detail:"Wi-Fi disconnects frequently in office.", Status:"Assigned", Days:-3),
            (Type:"Incident", Name:"Lerato Dube", Email:"lerato.dube@example.test", Category:"Printer", Detail:"Printer shows paper jam with no paper inside.", Status:"In Progress", Days:-4),
            (Type:"Incident", Name:"Musa Khumalo", Email:"musa.khumalo@example.test", Category:"SAP", Detail:"SAP session times out during stock movement.", Status:"Waiting for User", Days:-5),
            (Type:"Service", Name:"Zanele Pillay", Email:"zanele.pillay@example.test", Category:"Access / Permissions", Detail:"Finance shared-folder read access requested.", Status:"Resolved", Days:-6),
            (Type:"Equipment", Name:"Siyabonga Cele", Email:"siyabonga.cele@example.test", Category:"Laptop", Detail:"Replacement laptop required for onboarding.", Status:"New", Days:-2),
            (Type:"Equipment", Name:"Thando Naidoo", Email:"thando.naidoo@example.test", Category:"Monitor", Detail:"Second monitor required for daily reporting.", Status:"Assigned", Days:-7),
            (Type:"Service", Name:"Lerato Dube", Email:"lerato.dube@example.test", Category:"Software Installation", Detail:"Install approved PDF reader.", Status:"Closed", Days:-9),
            (Type:"Incident", Name:"External Test User", Email:"external@example.test", Category:"Software", Detail:"External-email ticket submission demonstration.", Status:"New", Days:-1),
            (Type:"Incident", Name:"External Test User", Email:"external@example.test", Category:"Software", Detail:"Related external-email ticket for merge testing.", Status:"New", Days:-1),
            (Type:"Service", Name:"Musa Khumalo", Email:"musa.khumalo@example.test", Category:"User Account", Detail:"Demo user account setup issue.", Status:"Waiting for Third Party", Days:-8),
            (Type:"Incident", Name:"Zanele Pillay", Email:"zanele.pillay@example.test", Category:"Hardware", Detail:"Laptop screen flickers intermittently.", Status:"Escalated to Just ICT", Days:-3),
            (Type:"Equipment", Name:"Anele Mokoena", Email:"anele.mokoena@example.test", Category:"Keyboard", Detail:"Keyboard replacement after key failure.", Status:"Resolved", Days:-10)
        };
        var ticketIds = new List<long>();
        foreach (var sample in scenarios)
        {
            var created = now.AddDays(sample.Days);
            var id = await db.ExecuteScalarAsync<long>(new CommandDefinition("""
                INSERT INTO AssetRequests(TicketNumber,RequestType,RequesterName,RequesterEmail,Branch,Department,
                                         ItemType,Quantity,Reason,Status,RequestedBy,RequestedAtUtc,LastUpdatedAtUtc)
                VALUES(NULL,@Type,@Name,@Email,'Phoenix','IT',@Category,1,@Detail,@Status,@Name,@created,@now);
                SELECT last_insert_rowid();
                """, new { sample.Type, sample.Name, sample.Email, sample.Category, sample.Detail, sample.Status, created, now }, tx, cancellationToken: ct));
            ticketIds.Add(id);
            await db.ExecuteAsync(new CommandDefinition(
                "UPDATE AssetRequests SET TicketNumber=@number WHERE Id=@id",
                new { id, number = $"RT-IT-{id:000000}" }, tx, cancellationToken: ct));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO TicketStatusHistory(TicketId,FromStatus,ToStatus,ChangedBy,ChangedByName,Comment,PublicVisible,ChangedAtUtc)
                VALUES(@id,NULL,'New',@actor,'RoyalDesk Demo','Demo ticket created (no email sent)',1,@created)
                """, new { id, actor = Actor, created }, tx, cancellationToken: ct));
            if (sample.Status != "New")
                await db.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO TicketStatusHistory(TicketId,FromStatus,ToStatus,ChangedBy,ChangedByName,Comment,PublicVisible,ChangedAtUtc)
                    VALUES(@id,'New',@status,@actor,'RoyalDesk Demo','Demo workflow status',1,@now)
                    """, new { id, status = sample.Status, actor = Actor, now }, tx, cancellationToken: ct));
        }

        // Connect active ticket scenarios to the seeded technician for realistic queues.
        var technicianId = await db.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT Id FROM ITTeamMembers WHERE lower(Username)='royaldesk.technician' AND IsActive=1 LIMIT 1",
            transaction: tx, cancellationToken: ct));
        if (technicianId.HasValue)
        {
            await db.ExecuteAsync(new CommandDefinition("""
                UPDATE AssetRequests
                SET AssignedToId=@technicianId, AssignedToName='RoyalDesk Test Technician',
                    AssignedAtUtc=@assigned,
                    StartedAtUtc=CASE WHEN Status IN ('In Progress','Waiting for User','Waiting for Third Party','Escalated to Just ICT','Resolved','Closed') THEN @assigned ELSE NULL END
                WHERE Id IN @ids
                """, new { technicianId, assigned = now.AddDays(-2),
                    ids = new[] { ticketIds[2],ticketIds[3],ticketIds[4],ticketIds[5],
                                  ticketIds[7],ticketIds[8],ticketIds[11],ticketIds[12],ticketIds[13] } },
                tx, cancellationToken: ct));
        }

        // One historical merged-ticket example, plus unmerged duplicates to test the merge UI.
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO TicketMerges(PrimaryTicketId,MergedTicketId,MergedBy,Reason,MergedAtUtc)
            VALUES(@primary,@child,@actor,'Duplicate demonstration records',@now)
            """, new { primary = ticketIds[9], child = ticketIds[10], actor = Actor, now }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE AssetRequests SET Status='Merged' WHERE Id=@id",
            new { id = ticketIds[10] }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition("""
            INSERT INTO TicketStatusHistory(TicketId,FromStatus,ToStatus,ChangedBy,ChangedByName,Comment,PublicVisible,ChangedAtUtc)
            VALUES(@id,'New','Merged',@actor,'RoyalDesk Demo','Merged into demonstration primary ticket',0,@now)
            """, new { id = ticketIds[10], actor = Actor, now }, tx, cancellationToken: ct));

        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO RoyalDeskDemoSeedRuns(SeedKey,SeededAtUtc) VALUES(@key,@now)",
            new { key = Marker, now }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }
}
