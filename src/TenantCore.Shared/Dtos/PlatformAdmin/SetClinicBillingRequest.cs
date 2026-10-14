using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// Internal — sets a clinic's billing mode and, optionally, its per-clinic registration fee and free
/// test days (both ride this one call, as the plan specifies).
///
/// <see cref="ApplyNow"/> true applies the mode change immediately and clears any queued change;
/// false queues it to the end of the clinic's current coverage, so days already paid for are kept.
/// Null <see cref="RegistrationFee"/> / <see cref="FreeTestDays"/> leave those values untouched.
/// </summary>
public record SetClinicBillingRequest
{
    public BillingMode BillingMode { get; init; }

    /// <summary>True applies the change now; false queues it to the end of the current coverage.</summary>
    public bool ApplyNow { get; init; }

    /// <summary>Per-clinic override of the plan's registration fee; null leaves the current value.</summary>
    public decimal? RegistrationFee { get; init; }

    /// <summary>Per-clinic free test days (30–90); null leaves the current value.</summary>
    public int? FreeTestDays { get; init; }

    /// <summary>
    /// Credit to store as pending on the clinic's account; released automatically once the clinic
    /// pays its registration fee. Only meaningful when <see cref="BillingMode"/> is PayAsYouGo and
    /// <see cref="RegistrationFee"/> is non-zero. Null leaves any existing pending credit unchanged.
    /// </summary>
    public decimal? InitialWalletCreditOnFeePayment { get; init; }

    public string? Reason { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}

