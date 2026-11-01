namespace TenantCore.Shared.Enums;

/// <summary>
/// Platform-side access state of one clinic. Integer values are a contract with
/// TenantCore.Admin (Core/Enums/ClinicAccessState.cs) — never renumber them.
/// </summary>
public enum ClinicAccessStatus
{
    Active = 1,
    Suspended = 2
}
