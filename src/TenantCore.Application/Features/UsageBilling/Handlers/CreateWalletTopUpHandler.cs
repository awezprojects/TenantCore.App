using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// Creates the payment row for a clinic-initiated wallet top-up. The Razorpay link itself is created
/// by the same background task every other payment uses, so a top-up adds no new payment path.
///
/// Crediting happens only when the gateway confirms — <c>CreditWalletTaskHandler</c> does that, and it
/// is idempotent on the payment id, so a webhook and a manual check cannot credit twice.
/// </summary>
public sealed class CreateWalletTopUpHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicAccountRepository accountRepository,
    IBillingContactResolver contactResolver,
    IPaymentLinkSuperseder linkSuperseder,
    IPaymentGateway paymentGateway,
    IOptions<UsageBillingOptions> options,
    ILogger<CreateWalletTopUpHandler> logger)
    : IRequestHandler<CreateWalletTopUpCommand, WalletTopUpPaymentDto>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task<WalletTopUpPaymentDto> Handle(CreateWalletTopUpCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        // The amount bounds are enforced in the validator; this is the last line of defence for a
        // caller that bypasses validation (an internal call, a future job).
        if (command.Amount < _options.TopUpMinAmount || command.Amount > _options.TopUpMaxAmount)
            throw new InvalidOperationException(
                $"A top-up must be between {_options.TopUpMinAmount:N2} and {_options.TopUpMaxAmount:N2}.");

        if (await accountRepository.IsSuspendedAsync(command.ApplicationId, ct))
            throw new InvalidOperationException("This clinic is suspended. Contact CloudClinic support.");

        var existingOpen = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);

        // Asking again for the same amount hands back the link that already exists rather than making
        // a second one — the clinic cannot end up with two live top-up links for one intention.
        if (existingOpen is not null
            && existingOpen.Purpose == PaymentPurpose.WalletTopUp
            && existingOpen.Amount == command.Amount
            && existingOpen.IsOpen)
            return UsageBillingTranslator.ToDto(existingOpen);

        // An admin-sent top-up is never replaced by the clinic itself — that is the admin's call.
        if (existingOpen is not null && existingOpen.InitiatedByAdminEmail is not null
            && existingOpen.Purpose == PaymentPurpose.WalletTopUp)
            throw new InvalidOperationException(
                "CloudClinic has sent this clinic a top-up link. Pay it, or contact support to have it changed.");

        var contact = await contactResolver.ResolveAsync(command.ApplicationId, command.ActingUserId, ct);

        var payment = SubscriptionPayment.CreateForWalletTopUp(
            command.ApplicationId, command.Amount, _options.Currency,
            contact.Name, contact.Email, contact.Phone, contact.ClinicName);

        await paymentRepository.AddAsync(payment, ct);
        await linkSuperseder.SupersedeOrCreateAsync(existingOpen, payment, ct);
        await paymentRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Wallet top-up {PaymentId} requested for clinic {ApplicationId}: {Amount} {Currency}.",
            payment.Id, command.ApplicationId, payment.Amount, payment.Currency);

        return UsageBillingTranslator.ToDto(payment);
    }
}
