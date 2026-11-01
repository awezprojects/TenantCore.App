using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Api.Authentication;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Translators;
using TenantCore.Shared.Dtos.Onboarding;
using TenantCore.Shared.Dtos.PlatformAdmin;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

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

    // ── Phase 2 — billing mode, rates, wallet and the registration fee ────────────────────────────

    [HttpPost("billing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetBillingMode(
        Guid applicationId, [FromBody] SetClinicBillingRequest request, CancellationToken ct)
    {
        await sender.Send(new SetClinicBillingCommand(
            applicationId, request.BillingMode, request.ApplyNow, request.RegistrationFee,
            request.FreeTestDays, request.Reason, request.AdminUserId, request.AdminEmail,
            InitialWalletCreditOnFeePayment: request.InitialWalletCreditOnFeePayment), ct);

        return NoContent();
    }

    [HttpPut("usage-rates")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetUsageRates(
        Guid applicationId, [FromBody] SetClinicUsageRatesRequest request, CancellationToken ct)
    {
        await sender.Send(new SetClinicUsageRatesCommand(
            applicationId, ToRateSettings(request.Items), request.Reason,
            request.AdminUserId, request.AdminEmail), ct);

        return NoContent();
    }

    [HttpPost("wallet/topup")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendWalletTopUp(
        Guid applicationId, [FromBody] ClinicWalletTopUpRequest request, CancellationToken ct)
    {
        var paymentId = await sender.Send(new SendClinicWalletTopUpCommand(
            applicationId, request.Amount, request.Reason, request.ReferenceId,
            request.AdminUserId, request.AdminEmail), ct);

        return StatusCode(StatusCodes.Status201Created, paymentId);
    }

    /// <summary>Returns the balance after the credit, so the portal can show the result of the action.</summary>
    [HttpPost("wallet/credit")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreditWallet(
        Guid applicationId, [FromBody] ClinicWalletCreditRequest request, CancellationToken ct)
    {
        var balance = await sender.Send(new CreditClinicWalletCommand(
            applicationId, request.Amount, request.Reason, request.ReferenceId,
            request.AdminUserId, request.AdminEmail), ct);

        return Ok(balance);
    }

    /// <summary>
    /// Admin sending (or re-sending) the clinic's one-time registration fee payment link.
    /// Returns 201 with the payment ID on success. Returns 409 if the fee is already paid or waived.
    /// </summary>
    [HttpPost("registration-fee/send-link")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRegistrationFeeLink(
        Guid applicationId, [FromBody] SendRegistrationFeeLinkRequest request, CancellationToken ct)
    {
        var contact = string.IsNullOrWhiteSpace(request.ContactEmail) ? null
            : new TenantCore.Application.Features.PlatformAdmin.Models.ClinicContact(
                request.ContactClinicName, request.ContactName, request.ContactEmail, request.ContactPhone);

        var paymentId = await sender.Send(new AdminSendRegistrationFeeLinkCommand(
            applicationId, request.AdminUserId, request.AdminEmail, contact), ct);

        return StatusCode(StatusCodes.Status201Created, paymentId);
    }

    [HttpPost("registration-fee/waive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> WaiveRegistrationFee(
        Guid applicationId, [FromBody] WaiveRegistrationFeeRequest request, CancellationToken ct)
    {
        await sender.Send(new WaiveRegistrationFeeCommand(
            applicationId, request.Reason, request.AdminUserId, request.AdminEmail), ct);

        return NoContent();
    }

    /// <summary>
    /// Triggers a Razorpay status check on the clinic's most recent registration fee payment.
    /// If the payment is already confirmed Paid by the gateway, re-enqueues RecordRegistrationFee
    /// so any deferred wallet credit that was skipped gets applied.
    /// If the link is still open (LinkCreated), enqueues ConfirmPayment to ask Razorpay now.
    /// </summary>
    [HttpPost("registration-fee/check-payment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckRegistrationFeePayment(
        Guid applicationId, [FromBody] AdminActionRequest request, CancellationToken ct)
    {
        await sender.Send(new AdminCheckRegistrationFeePaymentCommand(
            applicationId, request.AdminUserId, request.AdminEmail), ct);

        return NoContent();
    }

    /// <summary>
    /// Turns the request's operation names into the enum the command uses. An unknown name is a
    /// refused request, never a silently dropped rate — dropping one would leave a clinic paying the
    /// old price while the portal showed the new one.
    /// </summary>
    private static IReadOnlyList<UsageRateSetting> ToRateSettings(IReadOnlyList<ClinicUsageRateEntry> entries)
    {
        var settings = new List<UsageRateSetting>(entries.Count);

        foreach (var entry in entries)
        {
            if (!Enum.TryParse<BillingItem>(entry.BillingItem, ignoreCase: true, out var item))
                throw new InvalidOperationException($"Unknown billing item '{entry.BillingItem}'.");

            settings.Add(new UsageRateSetting(item, entry.Rate));
        }

        return settings;
    }
}
