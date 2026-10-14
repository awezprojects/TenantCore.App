using MediatR;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// Lets a Clinic Admin confirm a top-up immediately instead of waiting for the Razorpay webhook or
/// the reconciliation sweep. Enqueues the same idempotent ConfirmPayment task as every other check —
/// it re-reads the real status at the gateway, so the caller's word is never taken for payment.
/// </summary>
public sealed class CheckWalletTopUpHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    IOptions<UsageBillingOptions> options)
    : IRequestHandler<CheckWalletTopUpCommand>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task Handle(CheckWalletTopUpCommand command, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdForClinicAsync(command.PaymentId, command.ApplicationId, ct);

        // Another clinic's payment is treated as not found — never leak its existence.
        if (payment is null)
            throw new NotFoundException(nameof(SubscriptionPayment), command.PaymentId);

        if (payment.Purpose != PaymentPurpose.WalletTopUp)
            throw new InvalidOperationException("Only a wallet top-up can be checked here.");

        if (payment.Status != SubscriptionPaymentStatus.LinkCreated)
            throw new InvalidOperationException($"This top-up is {payment.Status} — there is nothing to check.");

        var throttle = TimeSpan.FromSeconds(_options.PaymentCheckThrottleSeconds);
        if (payment.LastCheckAt.HasValue && DateTime.UtcNow - payment.LastCheckAt.Value < throttle)
            throw new TooManyRequestsException("Please wait a moment before checking again.");

        payment.RecordCheck();

        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}",
            nameof(SubscriptionPayment), payment.Id, ct: ct);

        await paymentRepository.SaveChangesAsync(ct);
    }
}
