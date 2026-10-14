using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// The metering engine. One call per billable clinic operation: a <c>PayAsYouGo</c> clinic with a
/// priced item is debited from its wallet and refused with 403 <c>insufficient_wallet_balance</c>
/// when the wallet cannot cover it; a <c>Subscription</c> clinic returns immediately — no row, no
/// wallet touch — because subscription clinics are billed by their plan, not per-operation; an
/// unpriced item or a paused rollout writes a free row so usage analytics survive a mode change.
///
/// <b>This handler never calls <c>SaveChanges</c>.</b> That is deliberate and load-bearing: the usage
/// row and the wallet debit travel in the parent handler's transaction, so a refusal commits nothing
/// — no registration, no usage row, no partial debit. It also means a
/// <c>DbUpdateConcurrencyException</c> from two simultaneous charges surfaces at the parent's save;
/// re-reading the wallet and re-applying the debit once is the caller's responsibility.
/// </summary>
public sealed class ChargeUsageCommandHandler(
    IClinicUsageRecordRepository usageRecordRepository,
    IClinicUsageRateRepository usageRateRepository,
    IClinicWalletRepository walletRepository,
    IClinicWalletTransactionRepository walletTransactionRepository,
    IClinicAccountRepository accountRepository,
    IOptions<UsageBillingOptions> options,
    ILogger<ChargeUsageCommandHandler> logger)
    : IRequestHandler<ChargeUsageCommand, UsageChargeDto>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task<UsageChargeDto> Handle(ChargeUsageCommand command, CancellationToken ct)
    {
        var utcNow = command.OccurredAt ?? DateTime.UtcNow;

        // 1. Idempotency, before anything else. A retried request, a duplicated workflow task or a
        //    replayed call must never produce a second charge for the same operation. The unique index
        //    over (ApplicationId, BillingItem, SourceEntityId) is the backstop for a genuine race.
        var existing = await usageRecordRepository.GetBySourceAsync(
            command.ApplicationId, command.BillingItem, command.SourceEntityId, ct);

        if (existing is not null)
            return new UsageChargeDto(false, existing.Amount, null, existing.IsFree);

        // 2. Which mode is live. No account row means Subscription.
        //    Subscription clinics are billed by their plan, not per-operation — skip the record entirely
        //    so no usage rows, wallet rows or analytics are written for them. PAYG is the only mode that
        //    writes usage data.
        var account = await accountRepository.GetByApplicationIdAsync(command.ApplicationId, ct);
        var mode = account?.EffectiveBillingMode(utcNow) ?? BillingMode.Subscription;

        if (mode == BillingMode.Subscription)
            return new UsageChargeDto(false, 0m, null, true);

        // 3. The rate in force. No row means rate 0 means free — an unpriced item never blocks a clinic.
        var rate = await usageRateRepository.GetActiveAsync(command.ApplicationId, command.BillingItem, ct);
        var unitRate = rate?.Rate ?? 0m;

        // 4. The kill switch. Off still writes the row — free, with the rate recorded — so analytics
        //    survive a paused rollout and nothing needs backfilling when metering resumes.
        var metering = _options.Enabled && mode == BillingMode.PayAsYouGo && unitRate > 0m;

        var record = ClinicUsageRecord.Create(
            command.ApplicationId, command.BillingItem, command.SourceEntityId,
            unitRate, mode, utcNow, command.Quantity, command.UserId, forceFree: !metering);

        // 5. Staged, never saved here — the row travels in the parent handler's transaction.
        await usageRecordRepository.AddAsync(record, ct);

        if (!metering)
            return new UsageChargeDto(false, record.Amount, null, record.IsFree);

        // 6. Debit. A pay-as-you-go clinic that has never topped up gets a zero wallet created here,
        //    so its first charge fails with the explicit top-up message rather than a null reference.
        var wallet = await walletRepository.GetByApplicationIdAsync(command.ApplicationId, ct);
        if (wallet is null)
        {
            wallet = ClinicWallet.Create(
                command.ApplicationId, _options.DefaultLowBalanceThreshold, _options.Currency);
            await walletRepository.AddAsync(wallet, ct);
        }

        try
        {
            wallet.Debit(record.Amount, utcNow);
        }
        catch (DomainException)
        {
            // The domain stays HTTP-free; turning "insufficient" into 403 insufficient_wallet_balance
            // is an API concern (ADR-012 decision 12). Nothing has been saved, so nothing commits.
            throw new ErrorCodeException(
                403, UsageBillingErrorCodes.InsufficientWalletBalance,
                $"The clinic wallet does not have enough balance for {command.BillingItem}. Top up the wallet and try again.");
        }

        var ledgerRow = ClinicWalletTransaction.CreateUsageDebit(
            command.ApplicationId, record.Amount, wallet.Balance, record.Id,
            $"Usage — {command.BillingItem}", utcNow, command.UserId);

        await walletTransactionRepository.AddAsync(ledgerRow, ct);

        // Legal only because this row is not free — a free row must never point at a ledger movement
        // that does not exist.
        record.AttachWalletTransaction(ledgerRow.Id);

        logger.LogInformation(
            "Metered {BillingItem} for clinic {ApplicationId}: charged {Amount}, balance now {Balance}.",
            command.BillingItem, command.ApplicationId, record.Amount, wallet.Balance);

        return new UsageChargeDto(true, record.Amount, wallet.Balance, false);
    }
}
