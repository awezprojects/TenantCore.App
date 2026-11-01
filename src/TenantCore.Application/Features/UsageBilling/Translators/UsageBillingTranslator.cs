using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Translators;

/// <summary>
/// Static, by convention — never AutoMapper. Deterministic: the same entity always translates the
/// same way, so a retried query cannot produce a different DTO.
/// </summary>
public static class UsageBillingTranslator
{
    public static ClinicWalletDto ToDto(ClinicWallet wallet, IReadOnlyList<ClinicWalletTransaction> transactions) => new()
    {
        Balance = wallet.Balance,
        LowBalanceThreshold = wallet.LowBalanceThreshold,
        IsLow = wallet.IsLow,
        Currency = wallet.Currency,
        LastTopUpAt = wallet.LastTopUpAt,
        Transactions = transactions.Select(ToDto).ToList()
    };

    public static WalletTransactionDto ToDto(ClinicWalletTransaction transaction) => new()
    {
        OccurredAt = transaction.OccurredAt,
        Type = transaction.Type,
        Amount = transaction.Amount,
        BalanceAfter = transaction.BalanceAfter,
        ReferenceType = transaction.ReferenceType,
        Description = transaction.Description
    };

    public static UsageRateDto ToDto(ClinicUsageRate? rate, BillingItem billingItem) => new()
    {
        BillingItem = billingItem,
        Rate = rate?.Rate ?? 0m,
        IsPriced = rate is not null
    };

    public static UsageRecordDto ToDto(ClinicUsageRecord record) => new()
    {
        Id = record.Id,
        BillingItem = record.BillingItem,
        SourceEntityId = record.SourceEntityId,
        Quantity = record.Quantity,
        UnitRate = record.UnitRate,
        Amount = record.Amount,
        PayerMode = record.PayerMode,
        IsFree = record.IsFree,
        ChargedAt = record.ChargedAt
    };

    /// <summary>
    /// Builds the whole usage screen from the rows the handler already fetched, so the client needs a
    /// single query and every tab, sort and export is served from memory (workspace rule 7).
    /// </summary>
    public static ClinicUsageDto ToDto(
        IReadOnlyList<ClinicUsageRecord> records,
        IReadOnlyList<ClinicUsageTotals> totals,
        DateTime from, DateTime to, bool isCapped)
        => new()
        {
            From = from,
            To = to,
            TotalCharged = totals.Sum(t => t.Charged),
            TotalNotional = totals.Sum(t => t.Notional),
            Totals = totals
                .OrderByDescending(t => t.Charged)
                .ThenBy(t => t.BillingItem)
                .Select(t => new UsageItemTotalsDto
                {
                    BillingItem = t.BillingItem,
                    Count = t.Count,
                    Charged = t.Charged,
                    Notional = t.Notional
                })
                .ToList(),
            Series = BuildSeries(records),
            Records = records.Select(ToDto).ToList(),
            IsCapped = isCapped
        };

    /// <summary>The settled top-up's DTO. The link URL is only surfaced once the gateway has created it.</summary>
    public static WalletTopUpPaymentDto ToDto(SubscriptionPayment payment) => new()
    {
        PaymentId = payment.Id,
        Amount = payment.Amount,
        PaymentLinkUrl = payment.Status == SubscriptionPaymentStatus.LinkCreated ? payment.PaymentLinkUrl : null,
        Status = payment.Status
    };

    /// <summary>The registration-fee link's DTO. Same shape, same rule about the URL.</summary>
    public static RegistrationFeePaymentDto ToDto(SubscriptionPayment payment, bool asRegistrationFee) => new()
    {
        PaymentId = payment.Id,
        Amount = payment.Amount,
        PaymentLinkUrl = payment.Status == SubscriptionPaymentStatus.LinkCreated ? payment.PaymentLinkUrl : null,
        Status = payment.Status,
        LinkExpiresAt = payment.LinkExpiresAt
    };

    /// <summary>Daily buckets, oldest first, bucketed in UTC — the same day boundary the rows were written on.</summary>
    private static List<UsageSeriesPointDto> BuildSeries(IReadOnlyList<ClinicUsageRecord> records)
        => records
            .GroupBy(r => r.ChargedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new UsageSeriesPointDto
            {
                Date = g.Key,
                Count = g.Sum(r => r.Quantity),
                Charged = g.Sum(r => r.Amount),
                Notional = g.Sum(r => r.UnitRate * r.Quantity)
            })
            .ToList();
}
