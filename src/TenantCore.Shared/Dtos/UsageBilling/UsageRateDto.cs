using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// One billable operation's live price for a clinic. <see cref="IsPriced"/> false means no
/// <c>ClinicUsageRate</c> row exists, which means the operation is recorded free — never blocked.
/// </summary>
public record UsageRateDto
{
    public BillingItem BillingItem { get; init; }
    public decimal Rate { get; init; }
    public bool IsPriced { get; init; }
}
