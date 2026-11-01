using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// One range of metered usage, fetched once and then served entirely from the client's memory
/// (workspace rule 7): the page never re-queries on a tab, sort, filter or export.
/// </summary>
public record ClinicUsageDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>What the clinic actually paid over the range.</summary>
    public decimal TotalCharged { get; init; }

    /// <summary>What the same usage would have cost at the live rates — the analytics figure.</summary>
    public decimal TotalNotional { get; init; }

    public IReadOnlyList<UsageItemTotalsDto> Totals { get; init; } = [];

    /// <summary>Daily series for the inline-SVG trend chart.</summary>
    public IReadOnlyList<UsageSeriesPointDto> Series { get; init; } = [];

    /// <summary>The newest rows, capped server-side. <see cref="IsCapped"/> says whether the cap bit.</summary>
    public IReadOnlyList<UsageRecordDto> Records { get; init; } = [];

    /// <summary>True when more rows exist than were returned, so the UI can say so instead of truncating silently.</summary>
    public bool IsCapped { get; init; }
}

/// <summary>Per-item roll-up over the selected range.</summary>
public record UsageItemTotalsDto
{
    public BillingItem BillingItem { get; init; }
    public int Count { get; init; }

    /// <summary>What was charged for this item over the range.</summary>
    public decimal Charged { get; init; }

    /// <summary>Count × the rate that applied — includes free (subscription or unpriced) rows.</summary>
    public decimal Notional { get; init; }
}

/// <summary>One day of the trend series.</summary>
public record UsageSeriesPointDto
{
    public DateTime Date { get; init; }
    public int Count { get; init; }
    public decimal Charged { get; init; }
    public decimal Notional { get; init; }
}

/// <summary>
/// One metered operation. Deliberately free of any patient detail — <see cref="SourceEntityId"/> is a
/// registration GUID, never a name or MR number.
/// </summary>
public record UsageRecordDto
{
    public Guid Id { get; init; }
    public BillingItem BillingItem { get; init; }
    public Guid SourceEntityId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitRate { get; init; }
    public decimal Amount { get; init; }
    public BillingMode PayerMode { get; init; }
    public bool IsFree { get; init; }
    public DateTime ChargedAt { get; init; }
}
