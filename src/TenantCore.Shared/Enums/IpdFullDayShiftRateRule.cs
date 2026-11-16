namespace TenantCore.Shared.Enums;

// Resolves which rate a shift day bills under MidStayBedChangeChargeMode.FullDayCharge when the
// two rooms touched that day have different rates. Defaults to DestinationRoomRate at the clinic
// level (ClinicFeatureFlags.IpdFullDayShiftRateRule) because that's what the calculator already
// did unconditionally before this setting existed (always bill the room occupied at the end of
// the block) — so no clinic's bills change on upgrade day until this is changed.
public enum IpdFullDayShiftRateRule
{
    HigherRate = 1,
    DestinationRoomRate = 2,
    OriginRoomRate = 3
}
