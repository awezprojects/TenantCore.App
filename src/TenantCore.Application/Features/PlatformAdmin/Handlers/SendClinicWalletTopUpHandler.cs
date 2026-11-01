using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin sending a clinic a top-up link. Nothing is credited here: the clinic pays the link,
/// the gateway confirms it, and <c>CreditWalletTaskHandler</c> does the crediting — so an admin
/// "top-up" is still a real payment and the wallet ledger stays a record of settled money.
/// </summary>
public sealed class SendClinicWalletTopUpHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicAccountRepository accountRepository,
    IBillingContactResolver contactResolver,
    IPaymentLinkSuperseder linkSuperseder,
    IPaymentGateway paymentGateway,
    IOptions<UsageBillingOptions> options,
    ILogger<SendClinicWalletTopUpHandler> logger)
    : IRequestHandler<SendClinicWalletTopUpCommand, Guid>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task<Guid> Handle(SendClinicWalletTopUpCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        if (await accountRepository.IsSuspendedAsync(command.ApplicationId, ct))
            throw new InvalidOperationException("This clinic is suspended. Reactivate it before sending a top-up.");

        var existingOpen = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);

        // Idempotency: the same clinic, purpose and amount already live means this is a retry — hand
        // back the link that exists instead of opening a second one against the same intention.
        if (existingOpen is not null
            && existingOpen.Purpose == PaymentPurpose.WalletTopUp
            && existingOpen.Amount == command.Amount
            && existingOpen.IsOpen)
        {
            logger.LogInformation(
                "Wallet top-up for clinic {ApplicationId} already open as {PaymentId} — returning it.",
                command.ApplicationId, existingOpen.Id);
            return existingOpen.Id;
        }

        var contact = await contactResolver.ResolveAsync(command.ApplicationId, command.AdminUserId, ct);

        var payment = SubscriptionPayment.CreateForWalletTopUp(
            command.ApplicationId, command.Amount, _options.Currency,
            contact.Name, contact.Email, contact.Phone, contact.ClinicName,
            adminEmail: command.AdminEmail);

        await paymentRepository.AddAsync(payment, ct);
        await linkSuperseder.SupersedeOrCreateAsync(existingOpen, payment, ct);
        await paymentRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminEmail} sent wallet top-up {PaymentId} to clinic {ApplicationId}: {Amount} {Currency}.",
            command.AdminEmail, payment.Id, command.ApplicationId, payment.Amount, payment.Currency);

        return payment.Id;
    }
}
