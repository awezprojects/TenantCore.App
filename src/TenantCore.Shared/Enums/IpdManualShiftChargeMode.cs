namespace TenantCore.Shared.Enums;

// Who, if anyone, may enter a manual override amount for the room being left at shift time.
public enum IpdManualShiftChargeMode
{
    Off = 1,
    AdminOnly = 2,
    AdminAndReception = 3
}
