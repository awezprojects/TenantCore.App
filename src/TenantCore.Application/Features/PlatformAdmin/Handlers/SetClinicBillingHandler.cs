using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin switching a clinic's billing mode and, optionally, its per-clinic registration fee
/// and free test days.
///
/// <c>ApplyNow = true</c> applies immediately and clears any queued change. <c>ApplyNow = false</c>
/// queues the change to the end of the clinic's current coverage — the "take effect when the current
/// coverage ends" case — so days the clinic has already paid for are never lost by a change of mode.
///
/// A queued change can only be replaced by another queued change for the <b>same</b> mode (a later
/// date supersedes an earlier one). Queueing a different mode while one is unresolved is refused:
/// two pending modes have no defined meaning, and silently picking one would make the guard and the
/// screen disagree.
/// </summary>
public sealed class SetClinicBillingHandler(
    IClinicAccountRepository accountRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    ILogger<SetClinicBillingHandler> logger)
    : IRequestHandler<SetClinicBillingCommand>
{
    public async Task Handle(SetClinicBillingCommand command, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;

        // Tracked get-or-create: a clinic that has never needed an account row can still be switched.
        var account = await accountRepository.GetOrCreateAsync(command.ApplicationId, ct);

        // A queued change lands when the current coverage ends. With no coverage to protect there is
        // nothing to delay, so it applies now.
        var coverageEnd = command.ApplyNow
            ? null
            : await subscriptionRepository.GetCoverageEndAsync(command.ApplicationId, utcNow, ct);

        var applyNow = command.ApplyNow || coverageEnd is null;
        var changed = false;

        if (account.BillingMode != command.BillingMode || account.PendingBillingMode is not null)
        {
            if (!applyNow && account.PendingBillingMode is { } pending && pending != command.BillingMode)
            {
                throw new ErrorCodeException(
                    409,
                    UsageBillingErrorCodes.BillingModeLocked,
                    $"A change to {pending} is already queued until {account.PendingBillingModeEffectiveFrom:u}. "
                    + "Resolve it before queueing a different mode.");
            }

            account.SetBillingMode(command.BillingMode, utcNow, applyNow, applyNow ? null : coverageEnd);
            changed = true;
        }

        // The fee and the free test days ride the same call; both are optional and preserve the
        // current value when omitted.
        if (command.RegistrationFee is { } fee)
        {
            account.SetRegistrationFee(fee, command.AdminEmail);
            changed = true;
        }

        if (command.FreeTestDays is { } days)
        {
            account.SetFreeTestDays(days);
            changed = true;
        }

        // Deferred wallet credit: only stored when the caller explicitly passes it (activation-time
        // PAYG path). Admin-triggered billing mode changes always leave this field untouched (null
        // default means "don't change what's already stored").
        if (command.InitialWalletCreditOnFeePayment is { } pendingCredit && pendingCredit > 0m)
        {
            account.SetPendingWalletCreditOnFeePayment(pendingCredit);
            changed = true;
        }

        if (!changed)
        {
            logger.LogInformation(
                "Clinic {ApplicationId} is already on {Mode} with nothing queued and no fee/day change — no change written.",
                command.ApplicationId, command.BillingMode);
            return;
        }

        await accountRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminEmail} set clinic {ApplicationId} billing mode to {Mode} ({Timing}). Reason: {Reason}.",
            command.AdminEmail, command.ApplicationId, command.BillingMode,
            applyNow ? "effective now" : $"queued until {coverageEnd:u}",
            command.Reason ?? "-");
    }
}

