using MediatR;
using TenantCore.Application.Features.UsageBilling.Queries;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// The billing tab's single read. Every derived value is resolved server-side — the effective mode,
/// the resolved registration fee and the low-balance flag — so the client renders rather than decides.
/// </summary>
public sealed class GetClinicBillingSummaryHandler(
    IClinicAccountRepository accountRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IClinicUsageRateRepository usageRateRepository,
    IClinicWalletRepository walletRepository,
    IUsageBillingService usageBillingService)
    : IRequestHandler<GetClinicBillingSummaryQuery, ClinicBillingSummaryDto>
{
    public async Task<ClinicBillingSummaryDto> Handle(GetClinicBillingSummaryQuery query, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var applicationId = query.ApplicationId;

        // No row means the defaults — Subscription and the public catalogue — so a clinic that has
        // never needed an account keeps behaving exactly as it did before billing existed.
        var account = await accountRepository.GetByApplicationIdAsync(applicationId, ct);

        var coverageEnd = await subscriptionRepository.GetCoverageEndAsync(applicationId, utcNow, ct);

        // The fee belongs to the plan the clinic is actually on, so its catalogue default is read from
        // the most recent term rather than from whatever plan happens to be the current popular one.
        var latest = await subscriptionRepository.GetLatestForClinicAsync(applicationId, ct);
        var planDefaultFee = latest is null
            ? 0m
            : (await planRepository.GetByIdAsync(latest.SubscriptionPlanId, ct))?.RegistrationFee ?? 0m;

        var wallet = await walletRepository.GetByApplicationIdNoTrackingAsync(applicationId, ct);

        var effectiveMode = account?.EffectiveBillingMode(utcNow) ?? BillingMode.Subscription;
        var resolvedFee = usageBillingService.ResolveRegistrationFee(account?.RegistrationFee, planDefaultFee);

        var rates = await usageRateRepository.GetActiveForClinicAsync(applicationId, ct);
        var ratesByItem = rates.ToDictionary(r => r.BillingItem, r => r);

        return new ClinicBillingSummaryDto
        {
            BillingMode = account?.BillingMode ?? BillingMode.Subscription,
            EffectiveBillingMode = effectiveMode,
            PendingBillingMode = account?.PendingBillingMode,
            PendingBillingModeEffectiveFrom = account?.PendingBillingModeEffectiveFrom,
            CoverageEnd = coverageEnd,
            Currency = wallet?.Currency ?? "INR",
            WalletBalance = wallet?.Balance,
            LowBalanceThreshold = wallet?.LowBalanceThreshold,
            WalletIsLow = usageBillingService.IsLowBalance(wallet?.Balance, wallet?.LowBalanceThreshold),
            // Every billable item is reported, priced or not, so an unpriced operation is visible on
            // the screen instead of silently missing from it.
            Rates = Enum.GetValues<BillingItem>()
                .Select(item => UsageBillingTranslator.ToDto(ratesByItem.GetValueOrDefault(item), item))
                .ToList(),
            // A waived or already-charged fee is never due; a fee that resolves to zero is nothing to pay.
            RegistrationFeeDue = resolvedFee > 0m
                && account is not null
                && account.HasRegistrationFeeDue,
            RegistrationFee = resolvedFee,
            RegistrationFeeWaived = account?.RegistrationFeeWaived ?? false,
            RegistrationFeeChargedAt = account?.RegistrationFeeChargedAt,
            FreeTestDays = account?.FreeTestDays,
            TrialStartedAt = account?.TrialStartedAt
        };
    }
}
