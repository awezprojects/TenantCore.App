using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Tasks;

/// <summary>
/// Credits the clinic wallet once a <see cref="PaymentPurpose.WalletTopUp"/> payment is confirmed.
///
/// Idempotent twice over, deliberately: the <c>ExistsAsync</c> pre-check below catches a replayed
/// task, and the unique index on (ApplicationId, ReferenceType, ReferenceId) — with ReferenceId set
/// to the payment id — catches a genuine race. The balance and its ledger row are written in one
/// <c>SaveChanges</c>, because a credit without a ledger row is a bug, not an option.
/// </summary>
public sealed class CreditWalletTaskHandler(
    ISubscriptionPaymentRepository paymentRepository,
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    IOptions<UsageBillingOptions> options)
    : IWorkflowTaskHandler
{
    public WorkflowTaskType TaskType => WorkflowTaskType.CreditWallet;

    public async Task HandleAsync(WorkflowTask task, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
        if (payment is null)
            throw new PermanentWorkflowException($"SubscriptionPayment {task.AggregateId} not found.");

        if (payment.Purpose != PaymentPurpose.WalletTopUp)
            throw new PermanentWorkflowException($"Payment {payment.Id} is not a wallet top-up.");

        if (payment.Status != SubscriptionPaymentStatus.Paid)
            throw new InvalidOperationException("Payment is not marked Paid yet.");

        if (payment.ApplicationId is not { } applicationId)
            throw new PermanentWorkflowException($"Wallet top-up payment {payment.Id} has no clinic attached.");

        // Idempotency, first pass: a replay must not move money twice.
        if (await walletTransactionRepository.ExistsAsync(
                applicationId, WalletReferenceType.TopUpPayment, payment.Id, ct))
            return;

        var utcNow = DateTime.UtcNow;

        var wallet = await walletRepository.GetByApplicationIdAsync(applicationId, ct);
        if (wallet is null)
        {
            wallet = ClinicWallet.Create(
                applicationId, options.Value.DefaultLowBalanceThreshold, options.Value.Currency);
            await walletRepository.AddAsync(wallet, ct);
        }

        wallet.Credit(payment.Amount, utcNow);

        var ledgerRow = ClinicWalletTransaction.CreateTopUpCredit(
            applicationId, payment.Amount, wallet.Balance, payment.Id,
            "Wallet top-up", utcNow, payment.InitiatedByUserId);

        await walletTransactionRepository.AddAsync(ledgerRow, ct);

        // One save — the balance and the ledger row can never diverge.
        await walletRepository.SaveChangesAsync(ct);
    }
}
