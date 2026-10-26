using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.Authentication;
using RoyalDesk.Api.Lifecycle;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/lifecycle")]
[Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
public sealed class LifecycleController(ILifecycleRepository repository) : ControllerBase
{
    private string Actor => User.Identity?.Name ?? "unknown";

    [HttpGet("dashboard")]
    public Task<DashboardDto> Dashboard(CancellationToken ct) => repository.GetDashboardAsync(ct);

    [HttpGet("branches")]
    public Task<IReadOnlyList<LookupDto>> Branches(CancellationToken ct) => repository.GetBranchesAsync(ct);

    [HttpGet("departments")]
    public Task<IReadOnlyList<DepartmentDto>> Departments(CancellationToken ct) => repository.GetDepartmentsAsync(ct);

    [HttpGet("employees")]
    public Task<IReadOnlyList<EmployeeDto>> Employees(CancellationToken ct) => repository.GetEmployeesAsync(ct);

    [HttpPost("employees")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeNumber) || string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return BadRequest("Employee number, first name and last name are required.");
        return Ok(await repository.CreateEmployeeAsync(dto, ct));
    }

    [HttpGet("assets")]
    public Task<IReadOnlyList<AssetDto>> Assets(CancellationToken ct) => repository.GetAssetsAsync(ct);

    [HttpPost("assets")]
    public async Task<ActionResult<AssetDto>> CreateAsset(CreateAssetDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.AssetTag) || string.IsNullOrWhiteSpace(dto.Category))
            return BadRequest("Asset tag and category are required.");
        return Ok(await repository.CreateAssetAsync(dto, Actor, ct));
    }

    [HttpGet("assignments")]
    public Task<IReadOnlyList<AssignmentDto>> Assignments([FromQuery] int? employeeId, CancellationToken ct) => repository.GetAssignmentsAsync(employeeId, ct);

    [HttpPost("assignments")]
    public Task<AssignmentDto> Assign(AssignAssetDto dto, CancellationToken ct) => repository.AssignAssetAsync(dto, Actor, ct);

    [HttpPost("assignments/{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, AcceptAssetDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.SignatureName)) return BadRequest("Signature name is required.");
        await repository.AcceptAssetAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpPost("assignments/{id:int}/return")]
    public async Task<IActionResult> Return(int id, ReturnAssetDto dto, CancellationToken ct)
    {
        await repository.ReturnAssetAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpGet("assignments/{id:int}/acceptance.pdf")]
    public async Task<IActionResult> AcceptancePdf(int id, CancellationToken ct)
    {
        var assignment = await repository.GetAssignmentAsync(id, ct);
        if (assignment is null) return NotFound();
        return File(AcceptancePdfService.Create(assignment), "application/pdf", $"Employee_Acceptance_{assignment.AssetTag}_{id}.pdf");
    }

    [HttpGet("employees/{employeeId:int}/tasks/{lifecycleType}")]
    public Task<IReadOnlyList<LifecycleTaskDto>> Tasks(int employeeId, string lifecycleType, CancellationToken ct) => repository.GetTasksAsync(employeeId, NormaliseLifecycle(lifecycleType), ct);

    [HttpPatch("tasks/{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, UpdateLifecycleTaskDto dto, CancellationToken ct)
    {
        await repository.UpdateTaskAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpGet("write-offs")]
    public Task<IReadOnlyList<WriteOffDto>> WriteOffs(CancellationToken ct) => repository.GetWriteOffsAsync(ct);

    [HttpPost("write-offs")]
    public Task<WriteOffDto> CreateWriteOff(CreateWriteOffDto dto, CancellationToken ct) => repository.CreateWriteOffAsync(dto, Actor, ct);

    [HttpPost("write-offs/{id:int}/approval")]
    public async Task<IActionResult> ApproveWriteOff(int id, ApproveWriteOffDto dto, CancellationToken ct)
    {
        await repository.ApproveWriteOffAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpPost("write-offs/{id:int}/dispose")]
    public async Task<IActionResult> DisposeWriteOff(int id, DisposeWriteOffDto dto, CancellationToken ct)
    {
        await repository.DisposeWriteOffAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpPatch("asset-requests/{id:int}/workflow")]
    public async Task<IActionResult> UpdateRequestWorkflow(int id, UpdateRequestWorkflowDto dto, CancellationToken ct)
    {
        await repository.UpdateRequestWorkflowAsync(id, dto, Actor, ct);
        return NoContent();
    }

    [HttpPost("imports/e-waste")]
    public Task<ImportResultDto> ImportEwaste(IReadOnlyList<ImportEwasteRowDto> rows, CancellationToken ct) => repository.ImportEwasteAsync(rows, Actor, ct);

    private static string NormaliseLifecycle(string value) => value.Equals("offboarding", StringComparison.OrdinalIgnoreCase) ? "Offboarding" : "Onboarding";
}
