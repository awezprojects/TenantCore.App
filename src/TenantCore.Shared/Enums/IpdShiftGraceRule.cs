namespace TenantCore.Shared.Enums;

// How a bed/room shift within the clinic's grace window (ClinicFeatureFlags.IpdShiftGraceWindowMinutes)
// is billed, instead of the normal mid-stay charge mode. Only meaningful when the window is > 0.
public enum IpdShiftGraceRule
{
    NewRoomOnly = 1,
    LowerRate = 2,
    OldRoomOnly = 3
}
