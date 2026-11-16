using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/ipd-reports")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireClinicStaff)]
public class IpdReportsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("census")]
    [ProducesResponseType(typeof(IpdCensusReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCensus(CancellationToken ct)
        => Ok(await sender.Send(new GetIpdCensusReportQuery(GetApplicationId()), ct));

    [HttpGet("revenue")]
    [ProducesResponseType(typeof(IpdRevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenue([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdRevenueReportQuery(GetApplicationId(), fromUtc, toUtc), ct));

    [HttpGet("discharge-summary")]
    [ProducesResponseType(typeof(IpdDischargeSummaryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDischargeSummary([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdDischargeSummaryReportQuery(GetApplicationId(), fromUtc, toUtc), ct));
}
