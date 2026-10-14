using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Admin-side payment check for a clinic's one-time registration fee. Unlike the clinic-facing
/// <c>CheckRegistrationFeePaymentHandler</c>, this:
/// <list type="bullet">
///   <item>Has no rate-limiting (admin acts deliberately).</item>
///   <item>Handles the <c>Paid</c> status by re-enqueuing <c>RecordRegistrationFee</c> — useful when the
///     workflow task failed after the gateway confirmed payment (e.g. the deferred wallet-credit
///     validation error that used to block the task).</item>
///   <item>Handles the <c>LinkCreated</c> status by enqueuing <c>ConfirmPayment</c> to ask Razorpay now.</item>
/// </list>
/// </summary>
public sealed class AdminCheckRegistrationFeePaymentHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    ILogger<AdminCheckRegistrationFeePaymentHandler> logger)
    : IRequestHandler<AdminCheckRegistrationFeePaymentCommand>
{
    public async Task Handle(AdminCheckRegistrationFeePaymentCommand command, CancellationToken ct)
    {
        // Find the most recent registration fee payment for this clinic (any status).
        var payments = await paymentRepository.GetForClinicAsync(command.ApplicationId, ct);
        var payment = payments
            .Where(p => p.Purpose == PaymentPurpose.RegistrationFee)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (payment is null)
            throw new NotFoundException("RegistrationFeePayment", command.ApplicationId);

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            // Gateway already confirmed payment. Re-enqueue RecordRegistrationFee so any deferred
            // wallet credit that a previous task-run skipped gets applied (idempotent).
            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.RecordRegistrationFee,
                $"record-registration-fee:{payment.Id}",
                nameof(SubscriptionPayment), payment.Id, ct: ct);

            await paymentRepository.SaveChangesAsync(ct);

            logger.LogInformation(
                "Admin {AdminEmail} re-triggered RecordRegistrationFee for confirmed payment {PaymentId}, clinic {ApplicationId}.",
                command.AdminEmail, payment.Id, command.ApplicationId);

            return;
        }

        if (payment.Status == SubscriptionPaymentStatus.LinkCreated)
        {
            payment.RecordCheck();

            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.ConfirmPayment,
                $"confirm-payment:{payment.Id}",
                nameof(SubscriptionPayment), payment.Id, ct: ct);

            await paymentRepository.SaveChangesAsync(ct);

            logger.LogInformation(
                "Admin {AdminEmail} triggered ConfirmPayment for payment {PaymentId}, clinic {ApplicationId}.",
                command.AdminEmail, payment.Id, command.ApplicationId);

            return;
        }

        throw new InvalidOperationException(
            $"Registration fee payment {payment.Id} is {payment.Status} — it cannot be checked in this state.");
    }
}
