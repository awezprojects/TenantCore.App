using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

/// <summary>
/// Lets a Clinic Admin confirm a payment immediately instead of waiting for the Razorpay webhook
/// or the 15-minute reconciliation sweep. It never trusts the caller's word: it enqueues the same
/// idempotent ConfirmPayment task, which always re-checks the link's real status with Razorpay.
/// Throttled to once per 30 seconds per payment.
/// </summary>
public sealed class CheckSubscriptionPaymentHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IRequestHandler<CheckSubscriptionPaymentCommand>
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(30);

    public async Task Handle(CheckSubscriptionPaymentCommand command, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdForClinicAsync(command.PaymentId, command.ApplicationId, ct);

        // A payment belonging to another clinic is treated as not found — never leak it.
        if (payment is null)
            throw new NotFoundException(nameof(SubscriptionPayment), command.PaymentId);

        if (!payment.IsClinicLink)
            throw new InvalidOperationException("Only a renewal or admin-assigned payment can be checked here.");

        if (payment.Status != SubscriptionPaymentStatus.LinkCreated)
            throw new InvalidOperationException($"This payment is {payment.Status} — there is nothing to check.");

        if (payment.LastCheckAt.HasValue && DateTime.UtcNow - payment.LastCheckAt.Value < MinimumInterval)
            throw new TooManyRequestsException("Please wait a moment before checking again.");

        payment.RecordCheck();

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}",
            nameof(SubscriptionPayment), payment.Id, ct: ct);

        await paymentRepository.SaveChangesAsync(ct);
    }
}
