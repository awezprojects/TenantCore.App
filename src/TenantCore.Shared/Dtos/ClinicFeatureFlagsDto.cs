using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class ClinicFeatureFlagsDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public bool PrepaidOpdEnabled { get; init; }
    public bool BillingEnabled { get; init; }
    // Null = not yet configured by this clinic. Deliberately no default (ADR-010/plan decision,
    // confirmed 2026-11-12) — bed allocation is blocked until a Clinic Admin explicitly picks both.
    public RoomBillingCycle? IpdRoomBillingCycle { get; init; }
    public MidStayBedChangeChargeMode? IpdMidStayChargeMode { get; init; }
    // DEF-IPD-16 (13-Nov) — all four have defaults that reproduce pre-existing behavior exactly.
    public int IpdShiftGraceWindowMinutes { get; init; }
    public IpdShiftGraceRule? IpdShiftGraceRule { get; init; }
    public IpdManualShiftChargeMode IpdManualShiftChargeMode { get; init; }
    public IpdFullDayShiftRateRule IpdFullDayShiftRateRule { get; init; }
    // DEF-IPD-26 (15-Nov) — 0 = unlimited, the default that reproduces pre-existing behavior.
    public int IpdCancelWindowMinutes { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record UpdateClinicFeatureFlagsDto(
    bool PrepaidOpdEnabled,
    bool BillingEnabled,
    RoomBillingCycle? IpdRoomBillingCycle,
    MidStayBedChangeChargeMode? IpdMidStayChargeMode,
    int IpdShiftGraceWindowMinutes = 0,
    IpdShiftGraceRule? IpdShiftGraceRule = null,
    IpdManualShiftChargeMode IpdManualShiftChargeMode = IpdManualShiftChargeMode.Off,
    IpdFullDayShiftRateRule IpdFullDayShiftRateRule = IpdFullDayShiftRateRule.DestinationRoomRate,
    int IpdCancelWindowMinutes = 0);
