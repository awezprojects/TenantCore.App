using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class SubscriptionsController(ISender sender) : ClinicControllerBase
{
    [HttpGet("plans")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
        => Ok(await sender.Send(new GetSubscriptionPlansQuery(GetApplicationId()), ct));

    [HttpGet("status")]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
        => Ok(await sender.Send(new GetSubscriptionStatusQuery(GetApplicationId(), IsClinicAdmin()), ct));

    [HttpGet("history")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
        => Ok(await sender.Send(new GetSubscriptionHistoryQuery(GetApplicationId()), ct));

    [HttpPost("subscribe")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(typeof(ClinicSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        var command = new SubscribeToPlanCommand(GetApplicationId(), request.SubscriptionPlanId, GetCurrentUserId());
        var result = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetStatus), null, result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelSubscriptionCommand(GetApplicationId(), id, GetCurrentUserId()), ct);
        return NoContent();
    }

    // Checks every role the user holds for this clinic, not just one — a user commonly
    // holds more than one role for the same clinic (e.g. a doctor who self-registered
    // their own clinic is also that clinic's Clinic Admin), and GetCurrentUserRole()
    // alone would only see whichever role happened to appear first in the JWT claim.
    private bool IsClinicAdmin()
    {
        var roles = GetCurrentUserRoles();
        return roles.Any(role =>
            string.Equals(role, AppRoles.ClinicAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase));
    }
}
