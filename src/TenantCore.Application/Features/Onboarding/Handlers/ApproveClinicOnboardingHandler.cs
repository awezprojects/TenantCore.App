using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Application.Services;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class ApproveClinicOnboardingHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPlanRepository planRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<ApproveClinicOnboardingCommand>
{
    public async Task Handle(ApproveClinicOnboardingCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        if (request.Status != ClinicOnboardingStatus.Submitted)
            throw new InvalidOperationException($"Cannot approve a request in status {request.Status}.");

        if (command.GrantTrial)
        {
            request.ApproveWithTrial(command.ClinicCode, command.ReviewNote, command.AdminUserId, command.AdminEmail);
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id, ct: ct);

            await requestRepository.SaveChangesAsync(ct);
            return;
        }

        if (!paymentGateway.IsConfigured)
            throw new InvalidOperationException("Payments are not configured — cannot approve a paid plan yet.");

        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId!.Value, ct);
        if (plan == null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId!.Value);
        if (plan.IsTrial)
            throw new InvalidOperationException("Use GrantTrial to approve the Trial plan — it has no payment.");

        var amount = command.Amount!.Value;
        if (amount != plan.Price && string.IsNullOrWhiteSpace(command.AmountReason))
            throw new InvalidOperationException("A reason is required when the amount differs from the plan's list price.");

        request.ApproveWithPaidPlan(plan.Id, plan.Code, plan.Price, amount, command.AmountReason, command.ClinicCode, command.ReviewNote, command.AdminUserId, command.AdminEmail);

        var payment = SubscriptionPayment.CreateForOnboarding(
            request.Id, plan.Id, plan.Code, plan.Name, amount, plan.Price, plan.Currency,
            request.RequesterName, request.RequesterEmail, request.RequesterPhone);

        await paymentRepository.AddAsync(payment, ct);
        request.SetCurrentPayment(payment.Id);

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, $"create-link:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);

        // One shared ClinicDbContext behind both repositories — a single save commits everything.
        await requestRepository.SaveChangesAsync(ct);
    }
}
