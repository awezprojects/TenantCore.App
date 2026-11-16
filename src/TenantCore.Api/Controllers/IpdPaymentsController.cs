using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.IpdPayments.Commands;
using TenantCore.Application.Features.IpdPayments.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/ipd-payments")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireClinicStaff)]
public class IpdPaymentsController(ISender sender) : ClinicControllerBase
{
    [HttpPost("{ipdRegistrationId:guid}/discount")]
    [ProducesResponseType(typeof(IpdProvisionalBillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyDiscount(Guid ipdRegistrationId, [FromBody] ApplyIpdDiscountRequest request, CancellationToken ct)
        => Ok(await sender.Send(new ApplyIpdDiscountCommand(
            ipdRegistrationId, GetApplicationId(), request.Discount, GetCurrentUserId()), ct));

    // Live, never persisted — "amount payable if discharged today."
    [HttpGet("{ipdRegistrationId:guid}/provisional-bill")]
    [ProducesResponseType(typeof(IpdProvisionalBillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvisionalBill(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdProvisionalBillQuery(ipdRegistrationId, GetApplicationId()), ct));

    // Only available once the admission has been discharged (bill finalized).
    [HttpGet("{ipdRegistrationId:guid}/final-bill")]
    [ProducesResponseType(typeof(IpdFinalBillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetFinalBill(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new GetIpdFinalBillQuery(ipdRegistrationId, GetApplicationId()), ct));

    [HttpPost("{ipdRegistrationId:guid}/collect-balance")]
    [ProducesResponseType(typeof(IpdFinalBillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CollectBalance(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new CollectIpdBalanceCommand(ipdRegistrationId, GetApplicationId(), GetCurrentUserId()), ct));

    [HttpPost("{ipdRegistrationId:guid}/process-refund")]
    [ProducesResponseType(typeof(IpdFinalBillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessRefund(Guid ipdRegistrationId, CancellationToken ct)
        => Ok(await sender.Send(new ProcessIpdRefundCommand(ipdRegistrationId, GetApplicationId(), GetCurrentUserId()), ct));
}
