using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class ClinicFeatureFlags : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public bool PrepaidOpdEnabled { get; private set; }
    public bool BillingEnabled { get; private set; }
    // No default (confirmed) — null until a Clinic Admin explicitly picks one. CreateIpdRegistrationHandler
    // and TransferBedHandler block allocating a bed while either of these is null.
    public RoomBillingCycle? IpdRoomBillingCycle { get; private set; }
    public MidStayBedChangeChargeMode? IpdMidStayChargeMode { get; private set; }

    // DEF-IPD-16 (13-Nov): clinic-specific shift charging. All four default to the value that
    // reproduces today's behavior exactly, so no existing clinic's bills change until a Clinic
    // Admin deliberately opts into one of these.
    public int IpdShiftGraceWindowMinutes { get; private set; }
    public IpdShiftGraceRule? IpdShiftGraceRule { get; private set; }
    public IpdManualShiftChargeMode IpdManualShiftChargeMode { get; private set; } = IpdManualShiftChargeMode.Off;
    // Default is DestinationRoomRate, not HigherRate: the pre-existing calculator always picked
    // `overlapping[^1]` for a multi-room FullDayCharge block — i.e. the destination room,
    // unconditionally — which only happens to equal "higher rate" for a low→high shift (the only
    // direction anyone had observed before this setting existed). DestinationRoomRate is the one
    // choice that reproduces that old behavior exactly for BOTH directions, which the "bills match
    // today's, byte-for-byte" regression requirement depends on.
    public IpdFullDayShiftRateRule IpdFullDayShiftRateRule { get; private set; } = IpdFullDayShiftRateRule.DestinationRoomRate;

    // DEF-IPD-26 (15-Nov): how long after admission a cancellation stays free. 0 = unlimited
    // (today's behavior — Cancel always billed ₹0 regardless of elapsed time) so no existing
    // clinic's cancel policy changes until a Clinic Admin opts into a real cutoff. Past the
    // window, CancelIpdRegistrationHandler refuses the cancel outright — Discharge (which bills
    // for real) is the only path left.
    public int IpdCancelWindowMinutes { get; private set; }

    private ClinicFeatureFlags() { }

    public static ClinicFeatureFlags Create(Guid applicationId, bool prepaidOpdEnabled = true, bool billingEnabled = true) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        PrepaidOpdEnabled = prepaidOpdEnabled,
        BillingEnabled = billingEnabled,
        IpdManualShiftChargeMode = IpdManualShiftChargeMode.Off,
        IpdFullDayShiftRateRule = IpdFullDayShiftRateRule.DestinationRoomRate,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        bool prepaidOpdEnabled,
        bool billingEnabled,
        RoomBillingCycle? ipdRoomBillingCycle,
        MidStayBedChangeChargeMode? ipdMidStayChargeMode)
    {
        PrepaidOpdEnabled = prepaidOpdEnabled;
        BillingEnabled = billingEnabled;
        IpdRoomBillingCycle = ipdRoomBillingCycle;
        IpdMidStayChargeMode = ipdMidStayChargeMode;
        SetUpdatedAt();
    }

    public void UpdateShiftChargingSettings(
        int ipdShiftGraceWindowMinutes,
        IpdShiftGraceRule? ipdShiftGraceRule,
        IpdManualShiftChargeMode ipdManualShiftChargeMode,
        IpdFullDayShiftRateRule ipdFullDayShiftRateRule)
    {
        IpdShiftGraceWindowMinutes = Math.Max(0, ipdShiftGraceWindowMinutes);
        // A grace rule only means anything once the window is actually on — keep it null
        // otherwise so a stale leftover rule can never silently apply if the window is reopened
        // later without the admin re-picking a rule.
        IpdShiftGraceRule = IpdShiftGraceWindowMinutes > 0 ? ipdShiftGraceRule : null;
        IpdManualShiftChargeMode = ipdManualShiftChargeMode;
        IpdFullDayShiftRateRule = ipdFullDayShiftRateRule;
        SetUpdatedAt();
    }

    public void UpdateCancelWindow(int ipdCancelWindowMinutes)
    {
        IpdCancelWindowMinutes = Math.Max(0, ipdCancelWindowMinutes);
        SetUpdatedAt();
    }
}
