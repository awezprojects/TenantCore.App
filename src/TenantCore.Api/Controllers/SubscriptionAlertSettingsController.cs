using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Api.Controllers;

/// <summary>
/// Platform-level reminder-threshold configuration — NOT clinic-scoped, so this
/// does not inherit ClinicControllerBase and never reads X-Application-Id.
/// RequireClinicAdmin is reused here deliberately: ClinicRoleAuthorizationHandler
/// lets a System Admin role bypass the per-clinic check entirely (see its XML
/// doc), so with no clinic header on these routes only System Admin ever passes;
/// a plain Clinic Admin is denied because no clinic context exists to check
/// against. That is the intended "System Admin only" gate without introducing a
/// new authorization policy.
/// </summary>
[ApiController]
[Route("api/subscription-alert-settings")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
public class SubscriptionAlertSettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionAlertSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetSubscriptionAlertSettingsQuery(), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionAlertSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionAlertSettingRequest request, CancellationToken ct)
        => Ok(await sender.Send(new UpdateSubscriptionAlertSettingCommand(id, request), ct));
}
