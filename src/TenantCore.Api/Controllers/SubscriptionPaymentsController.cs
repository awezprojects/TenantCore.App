using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Clinic Admin payment history and renewal payment links. Routed under /api/subscriptions,
/// which SubscriptionGuardMiddleware already exempts — an expired clinic can still renew.
/// </summary>
[ApiController]
[Route("api/subscriptions/payments")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
public class SubscriptionPaymentsController(ISender sender) : ClinicControllerBase
{
    [HttpPost("links")]
    [ProducesResponseType(typeof(SubscriptionPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLink([FromBody] CreateRenewalPaymentLinkRequest request, CancellationToken ct)
    {
        var command = new CreateRenewalPaymentLinkCommand(GetApplicationId(), request.SubscriptionPlanId, GetCurrentUserId());
        var result = await sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetSubscriptionPaymentsQuery(GetApplicationId()), ct));

    /// <summary>
    /// "I've paid — check now". Does not trust the caller: it enqueues the same idempotent
    /// confirmation the Razorpay webhook would, so the real status still comes from Razorpay.
    /// </summary>
    [HttpPost("{id:guid}/check")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Check(Guid id, CancellationToken ct)
    {
        await sender.Send(new CheckSubscriptionPaymentCommand(GetApplicationId(), id, GetCurrentUserId()), ct);
        return NoContent();
    }
}
