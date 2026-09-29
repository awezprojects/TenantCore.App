using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Handlers;

/// <summary>
/// Replaces the current unpaid payment with a new one for the new amount. If the old payment
/// has no link yet, it's superseded immediately and the replacement's link is created directly.
/// If a link already exists, the old link is cancelled first — CancelPaymentLinkTaskHandler
/// creates the replacement's link only once that cancellation actually succeeds, and never if
/// the old link turns out to already be paid (the doctor can never pay twice).
/// </summary>
public sealed class ChangePaymentAmountHandler(
    IClinicOnboardingRequestRepository requestRepository,
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<ChangePaymentAmountCommand>
{
    public async Task Handle(ChangePaymentAmountCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(command.Id, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        if (request.Status is not (ClinicOnboardingStatus.Approved or ClinicOnboardingStatus.AwaitingPayment) || !request.CurrentPaymentId.HasValue)
            throw new InvalidOperationException($"Cannot change the amount from status {request.Status}.");

        var oldPayment = await paymentRepository.GetByIdAsync(request.CurrentPaymentId.Value, ct);
        if (oldPayment == null)
            throw new NotFoundException(nameof(SubscriptionPayment), request.CurrentPaymentId.Value);

        if (oldPayment.Status == SubscriptionPaymentStatus.Paid)
            throw new InvalidOperationException("This payment has already been paid — the amount can no longer be changed.");

        if (oldPayment.Amount == command.Amount)
            throw new InvalidOperationException("The new amount is the same as the current amount.");

        request.UpdateApprovedAmount(command.Amount, command.Reason);

        var replacement = SubscriptionPayment.CreateForOnboarding(
            request.Id, oldPayment.SubscriptionPlanId, oldPayment.PlanCode, oldPayment.PlanName,
            command.Amount, oldPayment.PlanListPrice, oldPayment.Currency,
            oldPayment.PayerName, oldPayment.PayerEmail, oldPayment.PayerPhone, oldPayment.Id);

        await paymentRepository.AddAsync(replacement, ct);
        request.SetCurrentPayment(replacement.Id);

        if (oldPayment.Status == SubscriptionPaymentStatus.Pending)
        {
            oldPayment.MarkSuperseded(replacement.Id);
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CreatePaymentLink, $"create-link:{replacement.Id}", nameof(SubscriptionPayment), replacement.Id, ct: ct);
        }
        else
        {
            oldPayment.SetReplacement(replacement.Id);
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CancelPaymentLink, $"cancel-link:{oldPayment.Id}", nameof(SubscriptionPayment), oldPayment.Id, ct: ct);
        }

        await requestRepository.SaveChangesAsync(ct);
    }
}
