using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

/// <summary>
/// Clinic Admin, clinic-scoped. Paid plans only, and only ones visible to this clinic.
///
/// One open link per clinic: asking again for the SAME plan returns the existing link, while
/// choosing a DIFFERENT plan supersedes it — otherwise a clinic that changed its mind would keep
/// being handed the old plan's link. A link an admin sent is never replaced by the clinic itself.
/// </summary>
public sealed class CreateRenewalPaymentLinkHandler(
    ISubscriptionPaymentRepository paymentRepository,
    ISubscriptionPlanRepository planRepository,
    IClinicAccountRepository accountRepository,
    IClinicPlanCatalog planCatalog,
    IAuthApplicationService authApplicationService,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<CreateRenewalPaymentLinkCommand, SubscriptionPaymentDto>
{
    public async Task<SubscriptionPaymentDto> Handle(CreateRenewalPaymentLinkCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        if (await accountRepository.IsSuspendedAsync(command.ApplicationId, ct))
            throw new InvalidOperationException("This clinic is suspended. Contact CloudClinic support.");

        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId, ct);
        if (plan == null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);
        if (plan.IsTrial)
            throw new InvalidOperationException("The Trial plan cannot be renewed through a payment link.");

        // A plan the clinic cannot see is treated as not found — never reveal a private package.
        if (!await planCatalog.IsVisibleAsync(command.ApplicationId, plan.Id, ct))
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);

        var amount = await planCatalog.GetEffectivePriceAsync(command.ApplicationId, plan, ct);

        var existingOpen = await paymentRepository.GetOpenLinkForClinicAsync(command.ApplicationId, ct);
        if (existingOpen is not null)
        {
            if (existingOpen.Purpose == PaymentPurpose.AdminAssigned)
                throw new InvalidOperationException(
                    "CloudClinic has sent this clinic a payment link. Pay it, or contact support to have it changed.");

            // Same plan and amount — hand back the link that already exists rather than making a second.
            if (existingOpen.SubscriptionPlanId == plan.Id && existingOpen.Amount == amount)
                return SubscriptionPaymentTranslator.ToDto(existingOpen);
        }

        var (email, name, phone, clinicName) = await ResolveBillingContactAsync(command.ApplicationId, command.ActingUserId, ct);

        var payment = SubscriptionPayment.CreateForRenewal(
            command.ApplicationId, plan.Id, plan.Code, plan.Name, amount, plan.Currency,
            name, email, phone, command.ActingUserId,
            listPrice: plan.Price, clinicName: clinicName);

        await paymentRepository.AddAsync(payment, ct);
        await SupersedeOrCreateAsync(existingOpen, payment, ct);

        await paymentRepository.SaveChangesAsync(ct);

        return SubscriptionPaymentTranslator.ToDto(payment);
    }

    /// <summary>
    /// Mirrors ChangePaymentAmountHandler: a link that exists at Razorpay must be cancelled there
    /// first (that task then creates the replacement's link), while one that was never created can
    /// be superseded immediately. If the old link turns out to be paid, the cancel task keeps that
    /// payment and drops the replacement, so the clinic can never pay twice.
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

    // Mirrors SubscribeToPlanHandler's billing-contact resolution — no persistent Auth-side
    // lookup exists for "who pays," so it's resolved fresh from the acting user + application.
    private async Task<(string Email, string Name, string? Phone, string? ClinicName)> ResolveBillingContactAsync(
        Guid applicationId, Guid actingUserId, CancellationToken ct)
    {
        var application = await authApplicationService.GetApplicationByIdAsync(applicationId, ct);
        var users = await authApplicationService.GetApplicationUsersAsync(applicationId, ct);
        var actingUser = users?.FirstOrDefault(u => u.UserId == actingUserId);

        var email = actingUser?.EmailId ?? application?.OfficialEmail ?? string.Empty;
        var name = actingUser?.FullName ?? application?.ContactPerson ?? application?.ApplicationName ?? string.Empty;
        var phone = actingUser?.MobileNo ?? application?.ContactNumber;

        return (email, name, phone, application?.ApplicationName);
    }
}
