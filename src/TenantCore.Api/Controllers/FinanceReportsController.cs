using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/finance-reports")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class FinanceReportsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(FinanceDashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? date = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
        => Ok(await sender.Send(new GetFinanceDashboardSummaryQuery(date ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId()), ct));

    [HttpGet("daily")]
    [ProducesResponseType(typeof(DailyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime? date = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
        => Ok(await sender.Send(new GetDailyCollectionReportQuery(date ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId()), ct));

    [HttpGet("weekly")]
    [ProducesResponseType(typeof(WeeklyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeekly([FromQuery] DateTime? weekStartDate = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
        => Ok(await sender.Send(new GetWeeklyCollectionReportQuery(weekStartDate ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId()), ct));

    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthly([FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow.AddMinutes(utcOffset);
        return Ok(await sender.Send(new GetMonthlyCollectionReportQuery(year ?? now.Year, month ?? now.Month, GetApplicationId()), ct));
    }

    [HttpGet("date-range")]
    [ProducesResponseType(typeof(DateRangeCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetDateRangeCollectionReportQuery(from, to, GetApplicationId(), utcOffset), ct));

    [HttpGet("reception-wise")]
    [ProducesResponseType(typeof(IEnumerable<ReceptionWiseReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceptionWise([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await sender.Send(new GetReceptionWiseReportQuery(from, to, GetApplicationId()), ct));

    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ExpenseSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetExpenseSummaryReportQuery(from, to, GetApplicationId(), utcOffset), ct));

    [HttpGet("doctor-wise")]
    [ProducesResponseType(typeof(DoctorWiseCollectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorWise(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        [FromQuery] Guid? doctorUserId = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetDoctorWiseOpdCollectionQuery(from, to, GetApplicationId(), utcOffset, doctorUserId), ct));
}
