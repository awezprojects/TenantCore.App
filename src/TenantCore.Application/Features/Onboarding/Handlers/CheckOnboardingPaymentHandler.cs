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
/// Owner-triggered "I've paid" check after Razorpay's post-payment redirect. Throttled to once
/// per 30 seconds. Never trusts the redirect itself — it only enqueues the same idempotent
/// ConfirmPayment task the webhook would, which always re-checks with Razorpay directly.
/// </summary>
public sealed class CheckOnboardingPaymentHandler(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<CheckOnboardingPaymentCommand>
{
    public async Task Handle(CheckOnboardingPaymentCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdForUserAsync(command.Id, command.UserId, ct);
        if (request == null)
            throw new NotFoundException(nameof(ClinicOnboardingRequest), command.Id);

        if (request.Status != ClinicOnboardingStatus.AwaitingPayment)
            throw new InvalidOperationException($"This request is not awaiting payment (status: {request.Status}).");

        if (request.LastPaymentCheckAt.HasValue && DateTime.UtcNow - request.LastPaymentCheckAt.Value < TimeSpan.FromSeconds(30))
            throw new TooManyRequestsException("Please wait a moment before checking again.");

        request.RecordPaymentCheck();

        if (request.CurrentPaymentId.HasValue)
        {
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ConfirmPayment, $"confirm-payment:{request.CurrentPaymentId.Value}",
                nameof(SubscriptionPayment), request.CurrentPaymentId.Value, ct: ct);
        }

        await requestRepository.SaveChangesAsync(ct);
    }
}
