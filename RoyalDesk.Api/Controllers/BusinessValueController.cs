using Microsoft.AspNetCore.Mvc;
using RoyalDesk.Api.BusinessValue;

namespace RoyalDesk.Api.Controllers;

[ApiController]
[Route("api/business-value")]
public sealed class BusinessValueController(BusinessValueService service) : ControllerBase
{
    [HttpGet]
    public Task<BusinessValueSummaryDto> Get(CancellationToken ct) => service.GetAsync(ct);
}
