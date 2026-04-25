namespace TenantCore.Shared.Authorization;

public static class AppRoles
{
    // Clinic roles
    public const string Pharmacist = "Pharmacist";
    public const string LabTechnician = "Lab Technician";
    public const string Nurse = "Nurse";
    public const string Doctor = "Doctor";
    public const string ClinicAdmin = "Clinic Admin";
    public const string ClinicManager = "Clinic Manager";
    public const string Receptionist = "Receptionist";

    // Super-admin (cross-application system administrator)
    public const string SystemAdmin = "System Admin";

    // Roles that can register/update patients and create OPD/IPD registrations
    public static readonly string[] ReceptionRoles =
        [Receptionist, ClinicAdmin, ClinicManager, SystemAdmin];

    // Roles that can perform clinical actions such as patient discharge
    public static readonly string[] ClinicalRoles =
        [Doctor, ClinicAdmin, ClinicManager, SystemAdmin];
}

public static class AuthPolicies
{
    // Any authenticated user (all clinic roles)
    public const string RequireAuthenticated = "RequireAuthenticated";

    // Clinic Admin only (fee config, admin-level settings)
    public const string RequireClinicAdmin = "RequireClinicAdmin";

    // Receptionist + Clinic Admin + Clinic Manager (register patients, create OPD/IPD)
    public const string RequireReception = "RequireReception";

    // Doctor + Clinic Admin + Clinic Manager (clinical decisions e.g. discharge)
    public const string RequireClinical = "RequireClinical";
}
