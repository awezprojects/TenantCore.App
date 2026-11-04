using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// The symmetric counterpart of <see cref="ChargeUsageCommandHandler"/> — credits back a metered
/// charge. Used when the operation it paid for never happened (an OPD/IPD registration cancelled
/// outright), <b>not</b> when a partial discount is applied to a visit that still goes ahead — the
/// platform usage charge is for registering the visit, not for the money collected from the patient.
///
/// Like <see cref="ChargeUsageCommandHandler"/>, this never calls <c>SaveChanges</c> — the reversal
/// travels in the parent handler's transaction.
/// </summary>
public sealed class ReverseUsageChargeCommandHandler(
    IClinicUsageRecordRepository usageRecordRepository,
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    ILogger<ReverseUsageChargeCommandHandler> logger)
    : IRequestHandler<ReverseUsageChargeCommand, UsageChargeDto>
{
    public async Task<UsageChargeDto> Handle(ReverseUsageChargeCommand command, CancellationToken ct)
    {
        var record = await usageRecordRepository.GetBySourceAsync(
            command.ApplicationId, command.BillingItem, command.SourceEntityId, ct);

        if (record is null || record.IsFree || record.Amount <= 0m)
            return new UsageChargeDto(false, 0m, null, true);

        // Idempotency pre-check — the same unique index ChargeUsageCommandHandler relies on,
        // checked proactively here instead of relying on a caught SQL error (DEF-OPD-05: a second
        // reversal attempt used to hit the unique index as a 409 and roll back the whole refund).
        var alreadyReversed = await walletTransactionRepository.ExistsAsync(
            command.ApplicationId, WalletReferenceType.UsageRefund, record.Id, ct);
        if (alreadyReversed)
            return new UsageChargeDto(false, record.Amount, null, false);

        var wallet = await walletRepository.GetByApplicationIdAsync(command.ApplicationId, ct);
        if (wallet is null)
            return new UsageChargeDto(false, record.Amount, null, false);

        var utcNow = command.OccurredAt ?? DateTime.UtcNow;
        wallet.Credit(record.Amount, utcNow);

        var ledgerRow = ClinicWalletTransaction.CreateUsageRefundCredit(
            command.ApplicationId, record.Amount, wallet.Balance, record.Id,
            $"Refund — {command.BillingItem}", utcNow, command.UserId);

        await walletTransactionRepository.AddAsync(ledgerRow, ct);

        logger.LogInformation(
            "Reversed usage charge for {BillingItem} {SourceEntityId} in clinic {ApplicationId}: credited {Amount}, balance now {Balance}.",
            command.BillingItem, command.SourceEntityId, command.ApplicationId, record.Amount, wallet.Balance);

        return new UsageChargeDto(true, record.Amount, wallet.Balance, false);
    }
}
