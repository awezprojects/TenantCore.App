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
/// "I've paid the registration fee — check now". Enqueues the same idempotent ConfirmPayment task as
/// every other check: the gateway's answer is the only thing that can mark the fee charged, and
/// <c>RecordRegistrationFeeTaskHandler</c> refuses a second charge.
/// </summary>
public sealed class CheckRegistrationFeePaymentHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    IOptions<UsageBillingOptions> options)
    : IRequestHandler<CheckRegistrationFeePaymentCommand>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task Handle(CheckRegistrationFeePaymentCommand command, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdForClinicAsync(command.PaymentId, command.ApplicationId, ct);

        // Another clinic's payment is treated as not found.
        if (payment is null)
            throw new NotFoundException(nameof(SubscriptionPayment), command.PaymentId);

        if (payment.Purpose != PaymentPurpose.RegistrationFee)
            throw new InvalidOperationException("Only a registration-fee payment can be checked here.");

        // Already confirmed — nothing left to do for the caller. The downstream
        // RecordRegistrationFee task handles the rest asynchronously.
        if (payment.Status == SubscriptionPaymentStatus.Paid)
            return;

        if (payment.Status != SubscriptionPaymentStatus.LinkCreated)
            throw new InvalidOperationException($"This payment is {payment.Status} — there is nothing to check.");

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
