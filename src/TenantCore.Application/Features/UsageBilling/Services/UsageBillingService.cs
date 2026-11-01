using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Services;

/// <inheritdoc />
public sealed class UsageBillingService(
    IClinicAccountRepository accountRepository,
    IClinicUsageRateRepository usageRateRepository)
    : IUsageBillingService
{
    /// <summary>
    /// A clinic with no account row is <see cref="BillingMode.Subscription"/> — exactly today's
    /// behaviour — and a row is never created just to answer the question.
    /// </summary>
    public async Task<BillingMode> ResolveEffectiveModeAsync(Guid applicationId, CancellationToken ct = default)
    {
        var account = await accountRepository.GetByApplicationIdAsync(applicationId, ct);
        return account?.EffectiveBillingMode(DateTime.UtcNow) ?? BillingMode.Subscription;
    }

    /// <summary>No row means rate 0 means free — an unpriced operation never blocks a clinic.</summary>
    public async Task<decimal> ResolveRateAsync(Guid applicationId, BillingItem billingItem, CancellationToken ct = default)
    {
        var rate = await usageRateRepository.GetActiveAsync(applicationId, billingItem, ct);
        return rate?.Rate ?? 0m;
    }

    public async Task<IReadOnlyDictionary<BillingItem, decimal>> ResolveRatesAsync(Guid applicationId, CancellationToken ct = default)
    {
        var rows = await usageRateRepository.GetActiveForClinicAsync(applicationId, ct);
        return rows.ToDictionary(r => r.BillingItem, r => r.Rate);
    }

    /// <summary>The clinic's own price wins; the catalogue default is only a fallback.</summary>
    public decimal ResolveRegistrationFee(decimal? clinicOverride, decimal planDefaultFee)
        => clinicOverride ?? planDefaultFee;

    /// <summary>At or below the threshold is low. Informational only — it never blocks an operation.</summary>
    public bool IsLowBalance(decimal? balance, decimal? threshold)
        => balance is { } funds && threshold is { } limit && funds <= limit;

    /// <summary>
    /// What this clinic would be charged for the item right now: its own rate, else the catalogue
    /// rate supplied by the caller, else free.
    /// </summary>
    public decimal ResolveChargeableAmount(decimal resolvedRate, int quantity)
        => resolvedRate <= 0m || quantity < 1
            ? 0m
            : Math.Round(resolvedRate * quantity, 2, MidpointRounding.AwayFromZero);
}
