using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

/// <summary>Clinic Admin, clinic-scoped. Paid plans only; an existing open link for the clinic is returned instead of creating a second one.</summary>
public sealed class CreateRenewalPaymentLinkHandler(
    ISubscriptionPaymentRepository paymentRepository,
    ISubscriptionPlanRepository planRepository,
    IAuthApplicationService authApplicationService,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<CreateRenewalPaymentLinkCommand, SubscriptionPaymentDto>
{
    public async Task<SubscriptionPaymentDto> Handle(CreateRenewalPaymentLinkCommand command, CancellationToken ct)
    {
        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured.");

        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId, ct);
        if (plan == null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);
        if (plan.IsTrial)
            throw new InvalidOperationException("The Trial plan cannot be renewed through a payment link.");

        var existingOpen = await paymentRepository.GetOpenRenewalForClinicAsync(command.ApplicationId, ct);
        if (existingOpen != null)
            return SubscriptionPaymentTranslator.ToDto(existingOpen);

        var (email, name, phone) = await ResolveBillingContactAsync(command.ApplicationId, command.ActingUserId, ct);

        var payment = SubscriptionPayment.CreateForRenewal(
            command.ApplicationId, plan.Id, plan.Code, plan.Name, plan.Price, plan.Currency,
            name, email, phone, command.ActingUserId);

        await paymentRepository.AddAsync(payment, ct);

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, $"create-link:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);

        await paymentRepository.SaveChangesAsync(ct);

        return SubscriptionPaymentTranslator.ToDto(payment);
    }

    // Mirrors SubscribeToPlanHandler's billing-contact resolution — no persistent Auth-side
    // lookup exists for "who pays," so it's resolved fresh from the acting user + application.
    private async Task<(string Email, string Name, string? Phone)> ResolveBillingContactAsync(Guid applicationId, Guid actingUserId, CancellationToken ct)
    {
        var application = await authApplicationService.GetApplicationByIdAsync(applicationId, ct);
        var users = await authApplicationService.GetApplicationUsersAsync(applicationId, ct);
        var actingUser = users?.FirstOrDefault(u => u.UserId == actingUserId);

        var email = actingUser?.EmailId ?? application?.OfficialEmail ?? string.Empty;
        var name = actingUser?.FullName ?? application?.ContactPerson ?? application?.ApplicationName ?? string.Empty;
        var phone = actingUser?.MobileNo ?? application?.ContactNumber;

        return (email, name, phone);
    }
}
