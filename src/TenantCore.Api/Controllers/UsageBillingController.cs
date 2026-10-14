using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TenantCore.Api.Export;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Queries;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Api.Controllers;

/// <summary>
/// The clinic's own pay-as-you-go surface: wallet, usage and the registration fee.
///
/// The route is <c>api/usage-billing</c> on purpose — plain "billing" is the clinic's patient-facing
/// billing module, and platform billing must never be confusable with it.
///
/// Reads are open to any authenticated clinic user (a receptionist needs to see why a charge was
/// refused); every money-moving action is Clinic-Admin only. The clinic comes from the
/// <c>X-Application-Id</c> header via <c>ClinicControllerBase</c>, never from the request body.
/// </summary>
[ApiController]
[Route("api/usage-billing")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class UsageBillingController(ISender sender, IOptions<UsageBillingOptions> options)
    : ClinicControllerBase
{
    private readonly UsageBillingOptions _options = options.Value;

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ClinicBillingSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
        => Ok(await sender.Send(new GetClinicBillingSummaryQuery(GetApplicationId()), ct));

    [HttpGet("wallet")]
    [ProducesResponseType(typeof(ClinicWalletDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWallet(CancellationToken ct)
        => Ok(await sender.Send(new GetWalletQuery(GetApplicationId()), ct));

    /// <summary>
    /// One range per call. The client caches the result and serves every tab, sort, filter and
    /// export from it, so secondary interactions never come back here (workspace rule 7).
    /// </summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(ClinicUsageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        // An omitted range is the recent past, not everything: the query is capped, and a caller
        // cannot ask for a window so wide that the cap silently hides the most recent usage.
        var rangeEnd = to ?? DateTime.UtcNow;
        var rangeStart = from ?? rangeEnd.Date.AddDays(-_options.DefaultUsageRangeDays);

        return Ok(await sender.Send(new GetClinicUsageQuery(GetApplicationId(), rangeStart, rangeEnd), ct));
    }

    [HttpPost("wallet/top-up")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(WalletTopUpPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTopUp([FromBody] CreateWalletTopUpRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateWalletTopUpCommand(GetApplicationId(), request.Amount, GetCurrentUserId()), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("wallet/top-up/{paymentId:guid}/check")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckTopUp(Guid paymentId, CancellationToken ct)
    {
        await sender.Send(new CheckWalletTopUpCommand(GetApplicationId(), paymentId), ct);
        return NoContent();
    }

    /// <summary>
    /// Formats the client's already-fetched usage data as a multi-sheet Excel workbook and returns it
    /// as a binary download. The client sends back the DTO it loaded on the primary Load action so
    /// nothing is re-queried (workspace rule 7).
    /// </summary>
    [HttpPost("usage/export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ExportExcel([FromBody] ExportUsageRequest request)
    {
        if (request.Data is null)
            return BadRequest("No usage data supplied.");

        var bytes = UsageExcelExporter.Build(request.Data, request.Currency);
        var from = request.Data.From.ToString("yyyy-MM-dd");
        var to = request.Data.To.ToString("yyyy-MM-dd");
        var fileName = $"usage_{from}_{to}.xlsx";

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("registration-fee/active")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(RegistrationFeePaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveRegistrationFeePayment(CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveRegistrationFeePaymentQuery(GetApplicationId()), ct);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpPost("registration-fee")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(RegistrationFeePaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRegistrationFeePayment(CancellationToken ct)
    {
        // No body: the amount is resolved server-side from the clinic's override or its plan, so the
        // party that owes the fee never gets to name it.
        var result = await sender.Send(
            new CreateRegistrationFeePaymentCommand(GetApplicationId(), GetCurrentUserId()), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("registration-fee/{paymentId:guid}/check")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckRegistrationFeePayment(Guid paymentId, CancellationToken ct)
    {
        await sender.Send(new CheckRegistrationFeePaymentCommand(GetApplicationId(), paymentId), ct);
        return NoContent();
    }
}
