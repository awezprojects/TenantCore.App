namespace TenantCore.Application.Features.UsageBilling.Models;

/// <summary>
/// What one metered operation cost. Returned by <c>ChargeUsageCommand</c> so a calling handler could
/// react to it; no caller branches on it today, but the amounts are asserted in tests.
/// </summary>
/// <param name="Charged">True when money actually moved out of the wallet.</param>
/// <param name="Amount">What was charged — 0 for a free (subscription or unpriced) row.</param>
/// <param name="BalanceAfter">The wallet balance afterwards, or null when nothing was charged.</param>
/// <param name="IsFree">True when the row is informational only.</param>
public sealed record UsageChargeDto(bool Charged, decimal Amount, decimal? BalanceAfter, bool IsFree);
