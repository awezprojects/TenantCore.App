using MediatR;
using Microsoft.Extensions.Logging;
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
/// Creates the payment row for the clinic's one-time registration fee. The fee is resolved
/// server-side — the clinic's own override, else its plan's catalogue default — so the amount is
/// never something the payer can supply.
///
/// A zero fee is not a payment: if nothing resolves, the clinic is told there is nothing to pay
/// rather than being sent a link for 0.
/// </summary>
public sealed class CreateRegistrationFeePaymentHandler(
    IClinicAccountRepository accountRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IBillingContactResolver contactResolver,
    IUsageBillingService usageBillingService,
    IPaymentLinkSuperseder linkSuperseder,
    IPaymentGateway paymentGateway,
    ILogger<CreateRegistrationFeePaymentHandler> logger)
    : IRequestHandler<CreateRegistrationFeePaymentCommand, RegistrationFeePaymentDto>
{
    public async Task<RegistrationFeePaymentDto> Handle(
        CreateRegistrationFeePaymentCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        if (await accountRepository.IsSuspendedAsync(command.ApplicationId, ct))
            throw new InvalidOperationException("This clinic is suspended. Contact CloudClinic support.");

        // Tracked get-or-create: the waive/charged flags live on this row, and the caller owns the save.
        var account = await accountRepository.GetOrCreateAsync(command.ApplicationId, ct);

        if (account.RegistrationFeeWaived)
            throw new InvalidOperationException("The registration fee has been waived — there is nothing to pay.");

        // Once-only by construction: a fee already settled is never charged a second time.
        if (account.RegistrationFeeChargedAt is not null)
            throw new InvalidOperationException("The registration fee has already been paid.");

        var latest = await subscriptionRepository.GetLatestForClinicAsync(command.ApplicationId, ct);
        var planDefaultFee = latest is null
            ? 0m
            : (await planRepository.GetByIdAsync(latest.SubscriptionPlanId, ct))?.RegistrationFee ?? 0m;

        var fee = usageBillingService.ResolveRegistrationFee(account.RegistrationFee, planDefaultFee);
        if (fee <= 0m)
            throw new InvalidOperationException("This clinic has no registration fee to pay.");

        var existingOpen = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);

        // Same fee and same purpose: hand back the link that already exists rather than making a second.
        if (existingOpen is not null
            && existingOpen.Purpose == PaymentPurpose.RegistrationFee
            && existingOpen.Amount == fee
            && existingOpen.IsOpen)
            return UsageBillingTranslator.ToDto(existingOpen, asRegistrationFee: true);

        var contact = await contactResolver.ResolveAsync(command.ApplicationId, command.ActingUserId, ct);

        var payment = SubscriptionPayment.CreateForRegistrationFee(
            command.ApplicationId, fee, account.Id == Guid.Empty ? "INR" : (latest?.Currency ?? "INR"),
            contact.Name, contact.Email, contact.Phone, contact.ClinicName);

        await paymentRepository.AddAsync(payment, ct);
        await linkSuperseder.SupersedeOrCreateAsync(existingOpen, payment, ct);
        await paymentRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Registration fee payment {PaymentId} requested for clinic {ApplicationId}: {Fee} {Currency}.",
            payment.Id, command.ApplicationId, payment.Amount, payment.Currency);

        return UsageBillingTranslator.ToDto(payment, asRegistrationFee: true);
    }
}
