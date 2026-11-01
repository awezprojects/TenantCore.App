using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin sending the registration fee link on behalf of a locked PAYG clinic. Mirrors the
/// logic in <c>CreateRegistrationFeePaymentHandler</c> but uses the clinic's own contact information
/// (admin user ID is not in the clinic's user list, so the resolver falls back to the application's
/// own details automatically) and logs the admin actor.
/// </summary>
public sealed class AdminSendRegistrationFeeLinkHandler(
    IClinicAccountRepository accountRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IBillingContactResolver contactResolver,
    IUsageBillingService usageBillingService,
    IPaymentLinkSuperseder linkSuperseder,
    IPaymentGateway paymentGateway,
    ILogger<AdminSendRegistrationFeeLinkHandler> logger)
    : IRequestHandler<AdminSendRegistrationFeeLinkCommand, Guid>
{
    public async Task<Guid> Handle(AdminSendRegistrationFeeLinkCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        if (await accountRepository.IsSuspendedAsync(command.ApplicationId, ct))
            throw new InvalidOperationException("This clinic is suspended. Reactivate it before sending a payment link.");

        var account = await accountRepository.GetOrCreateAsync(command.ApplicationId, ct);

        if (account.RegistrationFeeWaived)
            throw new InvalidOperationException("The registration fee has been waived — there is nothing to send.");

        if (account.RegistrationFeeChargedAt is not null)
            throw new InvalidOperationException("The registration fee has already been paid.");

        var latest = await subscriptionRepository.GetLatestForClinicAsync(command.ApplicationId, ct);
        var planDefaultFee = latest is null
            ? 0m
            : (await planRepository.GetByIdAsync(latest.SubscriptionPlanId, ct))?.RegistrationFee ?? 0m;

        var fee = usageBillingService.ResolveRegistrationFee(account.RegistrationFee, planDefaultFee);
        if (fee <= 0m)
            throw new InvalidOperationException("This clinic has no registration fee to pay.");

        // The Admin portal resolves the contact from Auth DB before the call so we avoid making a
        // user-JWT-authenticated Auth API call on a service-to-service request (which has no JWT).
        BillingContact contact;
        if (command.Contact is { } provided)
            contact = new BillingContact(provided.Name, provided.Email, provided.Phone, provided.ClinicName);
        else
            contact = await contactResolver.ResolveAsync(command.ApplicationId, command.AdminUserId, ct);

        var existingOpen = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);

        if (existingOpen is not null
            && existingOpen.Purpose == PaymentPurpose.RegistrationFee
            && existingOpen.Amount == fee
            && existingOpen.IsOpen)
        {
            logger.LogInformation(
                "Admin {AdminEmail} requested registration fee link for clinic {ApplicationId} — existing link {PaymentId} returned.",
                command.AdminEmail, command.ApplicationId, existingOpen.Id);
            return existingOpen.Id;
        }

        var currency = latest?.Currency ?? "INR";
        var payment = SubscriptionPayment.CreateForRegistrationFee(
            command.ApplicationId, fee, currency,
            contact.Name, contact.Email, contact.Phone, contact.ClinicName);

        await paymentRepository.AddAsync(payment, ct);
        await linkSuperseder.SupersedeOrCreateAsync(existingOpen, payment, ct);
        await paymentRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminEmail} sent registration fee link {PaymentId} to clinic {ApplicationId}: {Fee} {Currency}.",
            command.AdminEmail, payment.Id, command.ApplicationId, payment.Amount, currency);

        return payment.Id;
    }
}
