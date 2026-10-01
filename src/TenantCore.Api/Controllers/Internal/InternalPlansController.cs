using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Api.Authentication;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Translators;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Api.Controllers.Internal;

/// <summary>
/// The global plan catalogue, managed from the TenantCore.Admin portal. Guarded by the
/// "InternalService" scheme — plans are platform-wide, so this is never a clinic-scoped route.
/// </summary>
[ApiController]
[Route("api/internal/plans")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = InternalServiceKeyAuthenticationHandler.SchemeName)]
public class InternalPlansController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SaveSubscriptionPlanRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSubscriptionPlanCommand(
            PlatformAdminTranslator.ToPlanDetails(request), request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPut("{planId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid planId, [FromBody] SaveSubscriptionPlanRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateSubscriptionPlanCommand(
            planId, PlatformAdminTranslator.ToPlanDetails(request), request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("{planId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid planId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new SetSubscriptionPlanActiveCommand(
            planId, IsActive: true, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("{planId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid planId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new SetSubscriptionPlanActiveCommand(
            planId, IsActive: false, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }
}
