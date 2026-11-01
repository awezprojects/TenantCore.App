using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// The platform's own state for one clinic — access (suspended or not) and which plans it may see.
/// Tenant-scoped, at most one row per clinic.
///
/// Absence of a row means the defaults: Active, public catalogue. A row is created lazily by the
/// first admin action that needs one, so existing clinics need no backfill.
///
/// Suspension does NOT pause the subscription clock: the term keeps running while the clinic is
/// locked out (an admin compensates with a free grant if needed).
///
/// Phase 2 adds the billing state: which mode the clinic is billed in (subscription or
/// pay-as-you-go), its one-time registration fee and the package-granted free test days. None of it
/// is derived from the subscription rows — the subscription decides *whether* the clinic may work,
/// this decides *what it pays for*.
/// </summary>
public class ClinicAccount : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public ClinicAccessStatus AccessStatus { get; private set; }

    /// <summary>Shown to every user of the clinic while it is suspended.</summary>
    public string? SuspensionMessage { get; private set; }

    public DateTime? SuspendedAt { get; private set; }
    public string? SuspendedByAdminEmail { get; private set; }
    public DateTime? ReactivatedAt { get; private set; }
    public string? ReactivatedByAdminEmail { get; private set; }

    /// <summary>True hides the public catalogue — the clinic then sees only plans it has a live offer for.</summary>
    public bool RestrictToOfferedPlans { get; private set; }

    // ── Phase 2 — billing mode. The column default is Subscription, so a clinic that has never been
    // touched keeps behaving exactly as it did under Phase 1.

    /// <summary>The mode the clinic pays in. Always read through <see cref="EffectiveBillingMode"/>.</summary>
    public BillingMode BillingMode { get; private set; } = BillingMode.Subscription;

    /// <summary>A mode change queued to take effect when the current coverage ends; null when nothing is queued.</summary>
    public BillingMode? PendingBillingMode { get; private set; }

    /// <summary>UTC instant the queued change becomes live — the coverage end it was queued to.</summary>
    public DateTime? PendingBillingModeEffectiveFrom { get; private set; }

    // ── Phase 2 — one-time registration fee.

    /// <summary>Per-clinic override of the plan catalogue default; null means "use the plan's fee".</summary>
    public decimal? RegistrationFee { get; private set; }

    public DateTime? RegistrationFeeChargedAt { get; private set; }

    /// <summary>An admin email for a waive, or <c>payment:&lt;paymentId&gt;</c> when a Razorpay link settled it.</summary>
    public string? RegistrationFeeChargedBy { get; private set; }

    public bool RegistrationFeeWaived { get; private set; }

    public string? RegistrationFeeWaiveReason { get; private set; }

    public string? RegistrationFeeWaivedByAdminEmail { get; private set; }

    // ── Phase 2 — package-granted free test days.

    /// <summary>Package trial length in days (30–90 when set); null means "use the plan's duration".</summary>
    public int? FreeTestDays { get; private set; }

    /// <summary>Set the first time the clinic starts a trial; once set, no second trial is ever possible.</summary>
    public DateTime? TrialStartedAt { get; private set; }

    private ClinicAccount() { }

    public static ClinicAccount CreateDefault(Guid applicationId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        AccessStatus = ClinicAccessStatus.Active,
        RestrictToOfferedPlans = false,
        BillingMode = BillingMode.Subscription,
        CreatedAt = DateTime.UtcNow
    };

    public bool IsSuspended => AccessStatus == ClinicAccessStatus.Suspended;

    public void Suspend(string message, string adminEmail)
    {
        if (IsSuspended)
            throw new InvalidOperationException("This clinic is already suspended.");

        AccessStatus = ClinicAccessStatus.Suspended;
        SuspensionMessage = message;
        SuspendedAt = DateTime.UtcNow;
        SuspendedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }

    public void Reactivate(string adminEmail)
    {
        if (!IsSuspended)
            throw new InvalidOperationException("This clinic is not suspended.");

        AccessStatus = ClinicAccessStatus.Active;
        // SuspensionMessage is deliberately kept: the Admin portal shows what the last suspension said.
        ReactivatedAt = DateTime.UtcNow;
        ReactivatedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }

    public void SetRestrictToOfferedPlans(bool restrict)
    {
        RestrictToOfferedPlans = restrict;
        SetUpdatedAt();
    }

    // ── Phase 2 — billing mode ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The single definition of "which mode is live". A change queued to the end of the current
    /// coverage becomes live the moment <paramref name="utcNow"/> reaches its effective instant; until
    /// then the current mode keeps applying, so a clinic can never escape days it has already paid
    /// for. The guard, the metering command and the summary DTO all call this — never the raw column.
    /// </summary>
    public BillingMode EffectiveBillingMode(DateTime utcNow)
        => ResolveEffectiveBillingMode(BillingMode, PendingBillingMode, PendingBillingModeEffectiveFrom, utcNow);

    /// <summary>
    /// The rule behind <see cref="EffectiveBillingMode"/>, expressed so a repository can project just
    /// the three columns and apply <b>the same</b> logic without loading the row. There is deliberately
    /// one definition of the rule — a second, divergent copy is the classic way a guard and a screen
    /// start disagreeing about which mode a clinic is in.
    /// </summary>
    public static BillingMode ResolveEffectiveBillingMode(
        BillingMode current, BillingMode? pending, DateTime? pendingEffectiveFrom, DateTime utcNow)
        => pending is { } queued
           && pendingEffectiveFrom is { } effectiveFrom
           && effectiveFrom <= utcNow
            ? queued
            : current;

    /// <summary>
    /// Switches the billing mode. <paramref name="applyNow"/> is the admin's explicit "do not wait";
    /// otherwise the change is queued and takes effect when coverage ends — the caller resolves that
    /// instant and passes it as <paramref name="effectiveFrom"/>. A queued change is always
    /// superseded by a later one, and <see cref="ApplyPendingBillingMode"/> clears it on activation.
    /// </summary>
    public void SetBillingMode(BillingMode mode, DateTime utcNow, bool applyNow, DateTime? effectiveFrom = null)
    {
        if (applyNow)
        {
            BillingMode = mode;
            PendingBillingMode = null;
            PendingBillingModeEffectiveFrom = null;
            SetUpdatedAt();
            return;
        }

        PendingBillingMode = mode;
        // An unsubscribed clinic has no coverage to wait for, so the queue is immediate.
        PendingBillingModeEffectiveFrom = effectiveFrom ?? utcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// Makes a queued mode live. Called once a new term activates (and by the effective-mode read),
    /// so the switch lands exactly on the coverage boundary instead of relying on the clock alone.
    /// </summary>
    public void ApplyPendingBillingMode(DateTime utcNow)
    {
        if (PendingBillingMode is not { } pending)
            return;

        BillingMode = pending;
        PendingBillingMode = null;
        PendingBillingModeEffectiveFrom = null;
        SetUpdatedAt();
    }

    // ── Phase 2 — registration fee ────────────────────────────────────────────────────────────────

    /// <summary>
    /// True while the clinic still owes its one-time fee. Waived or already-charged is never due
    /// again; a resolve-to-zero fee is nothing to pay.
    /// </summary>
    public bool HasRegistrationFeeDue => !RegistrationFeeWaived && RegistrationFeeChargedAt is null;

    /// <summary>Sets the per-clinic override of the plan's catalogue fee. <paramref name="adminEmail"/> is the actor, for the caller's audit.</summary>
    public void SetRegistrationFee(decimal amount, string adminEmail)
    {
        RegistrationFee = amount;
        SetUpdatedAt();
    }

    /// <summary>Waives the fee, with a reason. A fee already paid is a manual refund, never a silent waive.</summary>
    public void WaiveRegistrationFee(string reason, string adminEmail)
    {
        if (RegistrationFeeWaived)
            throw new InvalidOperationException("The registration fee has already been waived.");

        if (RegistrationFeeChargedAt is not null)
            throw new InvalidOperationException("The registration fee has already been paid — refund it instead of waiving it.");

        RegistrationFeeWaived = true;
        RegistrationFeeWaiveReason = reason;
        RegistrationFeeWaivedByAdminEmail = adminEmail;
        SetUpdatedAt();
    }

    /// <summary>
    /// Records the one and only payment of the fee. Refuses a second charge outright — the fee is
    /// charged once, ever, per clinic.
    /// </summary>
    public void MarkRegistrationFeeCharged(string chargedBy, DateTime utcNow)
    {
        if (RegistrationFeeChargedAt is not null)
            throw new InvalidOperationException("The registration fee has already been charged.");

        if (RegistrationFeeWaived)
            throw new InvalidOperationException("The registration fee has been waived and can no longer be charged.");

        RegistrationFeeChargedAt = utcNow;
        RegistrationFeeChargedBy = chargedBy;
        SetUpdatedAt();
    }

    // ── Phase 2 — free test days ──────────────────────────────────────────────────────────────────

    /// <summary>Sets the package-granted trial length. 30–90 inclusive, per the plan's validation rule.</summary>
    public void SetFreeTestDays(int days)
    {
        if (days is < 30 or > 90)
            throw new DomainException("Free test days must be between 30 and 90.");

        FreeTestDays = days;
        SetUpdatedAt();
    }

    /// <summary>Marks the trial as started. Throws when the clinic has already had its one free trial.</summary>
    public void StartTrial(DateTime utcNow)
    {
        if (HasUsedTrial)
            throw new DomainException("This clinic has already used its free trial.");

        TrialStartedAt = utcNow;
        SetUpdatedAt();
    }

    /// <summary>Once a trial has started it can never be granted again, whatever the portal offers.</summary>
    public bool HasUsedTrial => TrialStartedAt is not null;

    // ── Phase 2 — deferred wallet credit ─────────────────────────────────────────────────────────

    /// <summary>
    /// Initial wallet credit deferred until the registration fee is paid. Set at activation for
    /// PAYG clinics that have a registration fee; cleared by <see cref="ClaimPendingWalletCreditOnFeePayment"/>
    /// once the payment is confirmed. Null means no deferred credit.
    /// </summary>
    public decimal? PendingWalletCreditOnFeePayment { get; private set; }

    /// <summary>Stores a credit to be unlocked once the registration fee is settled.</summary>
    public void SetPendingWalletCreditOnFeePayment(decimal amount)
    {
        PendingWalletCreditOnFeePayment = amount;
        SetUpdatedAt();
    }

    /// <summary>
    /// Reads the pending amount and clears it to null in one step. Returns null if there was nothing
    /// pending. Persisting the cleared state is the caller's responsibility.
    /// </summary>
    public decimal? ClaimPendingWalletCreditOnFeePayment()
    {
        var credit = PendingWalletCreditOnFeePayment;
        if (credit is null or <= 0m) return null;
        PendingWalletCreditOnFeePayment = null;
        SetUpdatedAt();
        return credit;
    }
}
