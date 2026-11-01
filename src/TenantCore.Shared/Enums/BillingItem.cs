namespace TenantCore.Shared.Enums;

/// <summary>
/// A metered clinic operation that a pay-as-you-go clinic is charged for.
///
/// Integer values are a contract with TenantCore.Admin and with
/// clinic.ClinicUsageRates / clinic.ClinicUsageRecords. Existing values are never renumbered.
/// </summary>
public enum BillingItem
{
    OpdRegistration = 1,
    IpdAdmission = 2,
    IpdDischarge = 3,
    OtSurgery = 4
}
