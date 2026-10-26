using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.Data;
using RoyalDesk.Api.Operations;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/operations")]
[Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
public sealed class OperationsController(IDbConnectionFactory connectionFactory) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? "unknown";

    [HttpGet("asset-requests")]
    public async Task<IReadOnlyList<AssetRequestWorkflowDto>> Requests(CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        const string sql = """
            SELECT Id,Branch,Department,ItemType,Quantity,Reason,RequestedBy,RequestedAtUtc,
                   Status,EmployeeId,ManagerApprovalStatus,ITStatus,ApprovedAtUtc,CompletedAtUtc
            FROM AssetRequests ORDER BY RequestedAtUtc DESC;
            """;
        return (await db.QueryAsync<AssetRequestWorkflowDto>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    [HttpGet("employees/{employeeId:int}/access")]
    public async Task<EmployeeAccessProfileDto> GetAccess(int employeeId, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        const string sql = """
            SELECT @employeeId EmployeeId, EmailAddress,SapUsername,CisTechUsername,VpnUsername,PhoneExtension,
                   COALESCE(UserGroupsAdded,0) UserGroupsAdded,COALESCE(EmailGroupsAdded,0) EmailGroupsAdded,
                   COALESCE(PrintScanConfigured,0) PrintScanConfigured,COALESCE(SharedFoldersConfigured,0) SharedFoldersConfigured,
                   COALESCE(PrintersConfigured,0) PrintersConfigured,Notes,UpdatedBy,UpdatedAtUtc
            FROM EmployeeAccessProfiles WHERE EmployeeId=@employeeId;
            """;
        var profile = await db.QuerySingleOrDefaultAsync<EmployeeAccessProfileDto>(new CommandDefinition(sql, new { employeeId }, cancellationToken: ct));
        return profile ?? new EmployeeAccessProfileDto(employeeId, null, null, null, null, null, false, false, false, false, false, null, null, null);
    }

    [HttpPut("employees/{employeeId:int}/access")]
    public async Task<IActionResult> SaveAccess(int employeeId, SaveEmployeeAccessProfileDto dto, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO EmployeeAccessProfiles(EmployeeId,EmailAddress,SapUsername,CisTechUsername,VpnUsername,PhoneExtension,
              UserGroupsAdded,EmailGroupsAdded,PrintScanConfigured,SharedFoldersConfigured,PrintersConfigured,Notes,UpdatedBy,UpdatedAtUtc)
            VALUES(@EmployeeId,@EmailAddress,@SapUsername,@CisTechUsername,@VpnUsername,@PhoneExtension,
              @UserGroupsAdded,@EmailGroupsAdded,@PrintScanConfigured,@SharedFoldersConfigured,@PrintersConfigured,@Notes,@Actor,@Now)
            ON CONFLICT(EmployeeId) DO UPDATE SET
              EmailAddress=excluded.EmailAddress,SapUsername=excluded.SapUsername,CisTechUsername=excluded.CisTechUsername,
              VpnUsername=excluded.VpnUsername,PhoneExtension=excluded.PhoneExtension,UserGroupsAdded=excluded.UserGroupsAdded,
              EmailGroupsAdded=excluded.EmailGroupsAdded,PrintScanConfigured=excluded.PrintScanConfigured,
              SharedFoldersConfigured=excluded.SharedFoldersConfigured,PrintersConfigured=excluded.PrintersConfigured,
              Notes=excluded.Notes,UpdatedBy=excluded.UpdatedBy,UpdatedAtUtc=excluded.UpdatedAtUtc;
            """;
        await db.ExecuteAsync(new CommandDefinition(sql, new { EmployeeId = employeeId, dto.EmailAddress, dto.SapUsername, dto.CisTechUsername, dto.VpnUsername, dto.PhoneExtension, dto.UserGroupsAdded, dto.EmailGroupsAdded, dto.PrintScanConfigured, dto.SharedFoldersConfigured, dto.PrintersConfigured, dto.Notes, Actor, Now = DateTime.UtcNow }, cancellationToken: ct));
        return NoContent();
    }

    [HttpPost("assets/{assetId:int}/transfer")]
    public async Task<IActionResult> Transfer(int assetId, TransferAssetDto dto, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var affected = await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET BranchId=@BranchId,DepartmentId=@DepartmentId,UpdatedAtUtc=@now WHERE Id=@assetId", new { assetId, dto.BranchId, dto.DepartmentId, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        if (affected == 0) return NotFound();
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'Transferred',@reason,@actor,@now)", new { assetId, reason = dto.Reason, actor = Actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpPost("assets/{assetId:int}/operational-status")]
    public async Task<IActionResult> OperationalStatus(int assetId, UpdateAssetOperationalStatusDto dto, CancellationToken ct)
    {
        var allowed = new[] { "Available", "In Repair", "Lost", "Stolen", "Pending Write-Off" };
        if (!allowed.Contains(dto.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest("Unsupported operational status.");
        await using var db = connectionFactory.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var affected = await db.ExecuteAsync(new CommandDefinition("UPDATE Assets SET Status=@Status,Condition=COALESCE(@Condition,Condition),Notes=COALESCE(@Notes,Notes),UpdatedAtUtc=@now WHERE Id=@assetId", new { assetId, dto.Status, dto.Condition, dto.Notes, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        if (affected == 0) return NotFound();
        await db.ExecuteAsync(new CommandDefinition("INSERT INTO AssetHistory(AssetId,EventType,Description,PerformedBy,CreatedAtUtc) VALUES(@assetId,'StatusChanged',@description,@actor,@now)", new { assetId, description = $"Status changed to {dto.Status}", actor = Actor, now = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpGet("assets/{assetId:int}/history")]
    public async Task<IReadOnlyList<AssetHistoryDto>> History(int assetId, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        return (await db.QueryAsync<AssetHistoryDto>(new CommandDefinition("SELECT Id,AssetId,EventType,Description,PerformedBy,CreatedAtUtc FROM AssetHistory WHERE AssetId=@assetId ORDER BY CreatedAtUtc DESC", new { assetId }, cancellationToken: ct))).AsList();
    }

    [HttpPost("employees/{employeeId:int}/complete-offboarding")]
    public async Task<IActionResult> CompleteOffboarding(int employeeId, CompleteOffboardingDto dto, CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        var openAssignments = await db.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM AssetAssignments WHERE EmployeeId=@employeeId AND Status='Assigned'", new { employeeId }, cancellationToken: ct));
        if (openAssignments > 0) return Conflict("Employee still has assigned assets. Return or resolve them before closing offboarding.");
        var incomplete = await db.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM EmployeeLifecycleTasks WHERE EmployeeId=@employeeId AND LifecycleType='Offboarding' AND IsCompleted=0", new { employeeId }, cancellationToken: ct));
        if (incomplete > 0) return Conflict("Offboarding checklist is not complete.");
        await db.ExecuteAsync(new CommandDefinition("UPDATE Employees SET EmploymentStatus='Inactive',EndDate=@EndDate,UpdatedAtUtc=@now WHERE Id=@employeeId", new { employeeId, dto.EndDate, now = DateTime.UtcNow }, cancellationToken: ct));
        return NoContent();
    }

    [HttpGet("reports/e-waste.csv")]
    public async Task<IActionResult> EwasteCsv(CancellationToken ct)
    {
        await using var db = connectionFactory.CreateConnection();
        var rows = await db.QueryAsync(new CommandDefinition("""
            SELECT a.SerialNumber,a.Make,a.Model,a.Category,w.Condition,w.CurrentValue Value,a.FullyDepreciated,
                   w.DisposalMethod,w.DisposalDate,w.ApprovedBy,w.Notes
            FROM WriteOffs w JOIN Assets a ON a.Id=w.AssetId
            WHERE w.ApprovalStatus='Approved' ORDER BY w.DisposalDate,w.Id;
            """, cancellationToken: ct));
        var sb = new StringBuilder("Serial Number,Make,Model,Category,Condition,Value,Fully Depreciated,Disposal Method,Disposal Date,Approved By,Notes\n");
        foreach (var row in rows)
        {
            var d = (IDictionary<string, object>)row;
            sb.AppendLine(string.Join(',', new[] { "SerialNumber","Make","Model","Category","Condition","Value","FullyDepreciated","DisposalMethod","DisposalDate","ApprovedBy","Notes" }.Select(k => Csv(d.TryGetValue(k, out var value) ? value : null))));
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"E-Waste-Asset-Write-Off-Register-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Csv(object? value)
    {
        var text = Convert.ToString(value) ?? string.Empty;
        return '"' + text.Replace("\"", "\"\"") + '"';
    }
}
