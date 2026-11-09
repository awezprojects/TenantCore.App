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
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(FinanceDashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? date = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetFinanceDashboardSummaryQuery(date ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("daily")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(DailyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime? date = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetDailyCollectionReportQuery(date ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("weekly")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(WeeklyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeekly([FromQuery] DateTime? weekStartDate = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetWeeklyCollectionReportQuery(weekStartDate ?? DateTime.UtcNow.AddMinutes(utcOffset).Date, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("monthly")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(MonthlyCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthly([FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        var now = DateTime.UtcNow.AddMinutes(utcOffset);
        return Ok(await sender.Send(new GetMonthlyCollectionReportQuery(year ?? now.Year, month ?? now.Month, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("date-range")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(DateRangeCollectionReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetDateRangeCollectionReportQuery(from, to, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("reception-wise")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(IEnumerable<ReceptionWiseReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceptionWise([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int utcOffset = 0, CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetReceptionWiseReportQuery(from, to, GetApplicationId(), utcOffset), ct));
    }

    [HttpGet("expenses")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(ExpenseSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetExpenseSummaryReportQuery(from, to, GetApplicationId(), utcOffset), ct));
    }

    // Admin/reception visibility only — who gave a discount matters for finance oversight
    // regardless of whether a doctor or reception applied it; doctors keep the ability to apply
    // a discount themselves (OpdPaymentsController.ApplyDiscount), they just don't get this
    // clinic-wide audit view.
    [HttpGet("discounts")]
    [Authorize(Policy = AuthPolicies.RequireReception)]
    [ProducesResponseType(typeof(OpdDiscountReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscounts(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        CancellationToken ct = default)
    {
        if (IsDoctorPanelView()) return Forbid();
        return Ok(await sender.Send(new GetOpdDiscountsReportQuery(from, to, GetApplicationId(), utcOffset), ct));
    }

    // Doctors need this one (to see their own collection), so it only requires clinic-staff
    // membership rather than a reception/admin role. To stop a doctor from reading another
    // doctor's earnings by passing someone else's id (the reported defect), any caller who
    // doesn't hold a role allowed to see every doctor's numbers for this clinic is always
    // scoped to themselves — a requested doctorUserId is honored only for
    // Receptionist/ClinicAdmin/ClinicManager/SystemAdmin (DEF-FB-06: reception bills for every
    // doctor and needs this the same as admin/manager; only a plain Doctor is self-scoped).
    // DEF-FB-03: a privileged role is also overridden to self-scope while the caller has
    // explicitly chosen to view as "Doctor" this session (IsDoctorPanelView()) — otherwise a
    // Doctor + Admin user calling this directly from "Doctor Panel" still got every doctor's data.
    [HttpGet("doctor-wise")]
    [Authorize(Policy = AuthPolicies.RequireClinicStaff)]
    [ProducesResponseType(typeof(DoctorWiseCollectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorWise(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int utcOffset = 0,
        [FromQuery] Guid? doctorUserId = null,
        CancellationToken ct = default)
    {
        var effectiveDoctorUserId = CanViewAnyDoctorsCollection() && !IsDoctorPanelView() ? doctorUserId : GetCurrentUserId();
        return Ok(await sender.Send(
            new GetDoctorWiseOpdCollectionQuery(from, to, GetApplicationId(), utcOffset, effectiveDoctorUserId), ct));
    }

    // Mirrors the GetCurrentUserRoles() pattern used elsewhere (e.g. SubscriptionsController) —
    // checks every role the user holds for this clinic, since a doctor who is also that clinic's
    // Clinic Admin must still be recognized as privileged here.
    private bool IsFinanceManager()
    {
        var roles = GetCurrentUserRoles();
        return roles.Any(r =>
            string.Equals(r, AppRoles.ClinicAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, AppRoles.ClinicManager, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase));
    }

    // Same as IsFinanceManager plus Receptionist — reception bills on behalf of every doctor in
    // the clinic, so they're allowed to look up any one doctor's collection by id, same as admin.
    private bool CanViewAnyDoctorsCollection()
        => IsFinanceManager() || GetCurrentUserRoles().Any(r =>
            string.Equals(r, AppRoles.Receptionist, StringComparison.OrdinalIgnoreCase));
}
