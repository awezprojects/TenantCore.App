namespace TenantCore.Shared.Enums;

// Why a room-charge line item's applied amount differs from its standard amount.
public enum IpdBillAdjustmentType
{
    None = 0,
    Grace = 1,
    Manual = 2
}
