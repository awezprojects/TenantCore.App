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
/// Everything the TenantCore.Admin portal can change about an existing clinic, guarded by the
/// "InternalService" scheme (X-Internal-Service-Key) — never reachable with a user JWT.
///
/// applicationId always comes from the route, never from a body: ClinicControllerBase and the
/// X-Application-Id header do not apply to a service-to-service caller, which acts across clinics.
/// The portal only ever sends ids it read from Auth's Applications table.
/// </summary>
[ApiController]
[Route("api/internal/clinics/{applicationId:guid}")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = InternalServiceKeyAuthenticationHandler.SchemeName)]
public class InternalClinicsController(ISender sender) : ControllerBase
{
    [HttpPost("suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Suspend(Guid applicationId, [FromBody] SuspendClinicRequest request, CancellationToken ct)
    {
        await sender.Send(new SuspendClinicCommand(
            applicationId, request.Message, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reactivate(Guid applicationId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new ReactivateClinicCommand(
            applicationId, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("subscriptions/grant")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantSubscription(
        Guid applicationId, [FromBody] GrantClinicSubscriptionRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new GrantClinicSubscriptionCommand(
            applicationId, request.SubscriptionPlanId, request.Reason,
            PlatformAdminTranslator.ToContact(request.Contact),
            request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPost("subscriptions/payment-link")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignPlanPaymentLink(
        Guid applicationId, [FromBody] AssignPlanPaymentLinkRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new AssignPlanPaymentLinkCommand(
            applicationId, request.SubscriptionPlanId, request.Amount, request.AmountReason,
            PlatformAdminTranslator.ToContact(request.Contact),
            request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPost("subscriptions/{subscriptionId:guid}/cancel-upcoming")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelUpcomingSubscription(
        Guid applicationId, Guid subscriptionId, [FromBody] CancelUpcomingSubscriptionRequest request, CancellationToken ct)
    {
        await sender.Send(new CancelUpcomingSubscriptionCommand(
            applicationId, subscriptionId, request.Reason, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("payments/{paymentId:guid}/cancel-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPaymentLink(
        Guid applicationId, Guid paymentId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new CancelClinicPaymentLinkCommand(
            applicationId, paymentId, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPut("plan-visibility")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetPlanVisibility(
        Guid applicationId, [FromBody] SetClinicPlanVisibilityRequest request, CancellationToken ct)
    {
        await sender.Send(new SetClinicPlanVisibilityCommand(
            applicationId, request.RestrictToOfferedPlans, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }

    [HttpPost("plan-offers")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlanOffer(
        Guid applicationId, [FromBody] CreateClinicPlanOfferRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateClinicPlanOfferCommand(
            applicationId, request.SubscriptionPlanId, request.OfferPrice, request.ValidUntil,
            request.Note, request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPost("plan-offers/{offerId:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> WithdrawPlanOffer(
        Guid applicationId, Guid offerId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new WithdrawClinicPlanOfferCommand(
            applicationId, offerId, request.AdminUserId, request.AdminEmail), ct);
        return NoContent();
    }
}
