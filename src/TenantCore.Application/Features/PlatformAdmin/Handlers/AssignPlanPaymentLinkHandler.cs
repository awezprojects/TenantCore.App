using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Translators;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// An internal admin sending an existing clinic a payment link for a plan and amount they chose.
/// The plan does not have to be one the clinic can see for itself — an admin may assign any active
/// paid plan, which is exactly how a private package is sold.
///
/// At most one link is open per clinic: an existing one is superseded using the same mechanism as
/// change-amount, so a clinic can never be shown two payable links at once.
/// </summary>
public sealed class AssignPlanPaymentLinkHandler(
    ISubscriptionPlanRepository planRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IClinicPlanCatalog planCatalog,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer,
    ILogger<AssignPlanPaymentLinkHandler> logger)
    : IRequestHandler<AssignPlanPaymentLinkCommand, Guid>
{
    public async Task<Guid> Handle(AssignPlanPaymentLinkCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);

        if (plan.IsTrial)
            throw new InvalidOperationException("The Trial plan has no price — grant it instead of sending a payment link.");

        // Compared against what THIS clinic would normally pay (its live offer price, else the
        // list price), so a discount off a special price still needs an explicit reason.
        var effectivePrice = await planCatalog.GetEffectivePriceAsync(command.ApplicationId, plan, ct);
        var amountReason = PlatformAdminTranslator.NormalizeOptionalText(command.AmountReason);

        if (command.Amount != effectivePrice && amountReason is null)
            throw new InvalidOperationException("A reason is required when the amount differs from the clinic's price for this plan.");

        var payment = SubscriptionPayment.CreateForAdminAssignment(
            command.ApplicationId, plan.Id, plan.Code, plan.Name,
            command.Amount, effectivePrice, plan.Currency,
            command.Contact.ClinicName, command.Contact.Name, command.Contact.Email, command.Contact.Phone,
            command.AdminEmail, amountReason);

        await paymentRepository.AddAsync(payment, ct);

        var openLink = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);
        await SupersedeOrCreateAsync(openLink, payment, ct);

        await paymentRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin assigned plan {PlanId} to clinic {ApplicationId} as payment {PaymentId} (superseding {SupersededPaymentId}).",
            plan.Id, command.ApplicationId, payment.Id, openLink?.Id);

        return payment.Id;
    }

    /// <summary>
    /// Mirrors ChangePaymentAmountHandler: a link that exists at Razorpay must be cancelled there
    /// first (the cancel task then creates the replacement's link), while one that was never
    /// created can be superseded immediately.
    /// </summary>
    private async Task SupersedeOrCreateAsync(SubscriptionPayment? openLink, SubscriptionPayment replacement, CancellationToken ct)
    {
        if (openLink is null || openLink.Status == SubscriptionPaymentStatus.Pending)
        {
            openLink?.MarkSuperseded(replacement.Id);

            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CreatePaymentLink, $"create-link:{replacement.Id}",
                nameof(SubscriptionPayment), replacement.Id, ct: ct);
            return;
        }

        openLink.SetReplacement(replacement.Id);
        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{openLink.Id}",
            nameof(SubscriptionPayment), openLink.Id, ct: ct);
    }
}
