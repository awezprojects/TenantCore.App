using MediatR;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

/// <summary>
/// Marks the one-time registration fee charged once its
/// <see cref="PaymentPurpose.RegistrationFee"/> payment is confirmed. Also applies any wallet
/// credit that was deferred at activation time (PAYG clinics whose fee must be paid first).
///
/// Idempotency:
///   - Fee marking: <c>ClinicAccount.MarkRegistrationFeeCharged</c> refuses a second charge and
///     <c>HasRegistrationFeeDue</c> acts as a fast-path guard.
///   - Wallet credit: <c>CreditClinicWalletCommand</c> is idempotent on <c>payment.Id</c>, so a
///     retry never double-credits. The pending-credit field is cleared only after the credit succeeds,
///     so a crash between the two saves leaves the field set and the retry re-enters the credit path.
/// </summary>
public sealed class RecordRegistrationFeeTaskHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicAccountRepository accountRepository,
    ISender sender)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.RecordRegistrationFee;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
        if (payment is null)
            throw new PermanentWorkflowException($"SubscriptionPayment {task.AggregateId} not found.");

        if (payment.Purpose != PaymentPurpose.RegistrationFee)
            throw new PermanentWorkflowException($"Payment {payment.Id} is not a registration fee.");

        if (payment.Status != SubscriptionPaymentStatus.Paid)
            throw new InvalidOperationException("Payment is not marked Paid yet.");

        if (payment.ApplicationId is not { } applicationId)
            throw new PermanentWorkflowException($"Registration fee payment {payment.Id} has no clinic attached.");

        var account = await accountRepository.GetByApplicationIdAsync(applicationId, ct);

        // No account row means the clinic's fee was only ever resolved from the plan default —
        // there is nothing to mark, and creating a row here would be inventing state.
        if (account is null)
            return;

        // Truly done: fee charged AND no deferred wallet credit remaining.
        if (!account.HasRegistrationFeeDue
            && (account.PendingWalletCreditOnFeePayment is null || account.PendingWalletCreditOnFeePayment <= 0m))
            return;

        // Step 1 — Mark fee charged (idempotent: refused if already charged).
        if (account.HasRegistrationFeeDue)
        {
            account.MarkRegistrationFeeCharged($"payment:{payment.Id}", DateTime.UtcNow);
            await accountRepository.SaveChangesAsync(ct);  // Save A: fee recorded
        }

        // Step 2 — Apply the deferred wallet credit, if any. CreditClinicWalletCommand is
        // idempotent on payment.Id, so a retry after Save A but before Save B is safe.
        var pendingCredit = account.PendingWalletCreditOnFeePayment;
        if (pendingCredit is { } credit && credit > 0m)
        {
            await sender.Send(new CreditClinicWalletCommand(
                applicationId,
                credit,
                "Initial wallet credit — registration fee paid",
                payment.Id,               // idempotency key: unique per payment
                Guid.Empty,               // automated — no human actor; Guid.Empty is the system sentinel
                "system@tenantcore.internal"), ct);

            account.ClaimPendingWalletCreditOnFeePayment();   // clear the field
            await accountRepository.SaveChangesAsync(ct);     // Save B: pending credit cleared
        }
    }
}
