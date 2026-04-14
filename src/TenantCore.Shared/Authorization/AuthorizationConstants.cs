namespace TenantCore.Shared.Authorization;

/// <summary>
/// Defines application role names used for authorization across all layers.
/// </summary>
public static class AppRoles
{
    // Admin roles
    public const string SystemAdmin = "System Admin";
    public const string ClinicAdmin = "Clinic Admin";
    public const string SchoolAdmin = "School Admin";

    // Standard roles
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string User = "User";

    // Viewer role
    public const string Viewer = "Viewer";

    /// <summary>
    /// All admin-level roles.
    /// </summary>
    public static readonly string[] AdminRoles = [SystemAdmin, ClinicAdmin, SchoolAdmin];

    /// <summary>
    /// All management-level roles (admins + managers).
    /// </summary>
    public static readonly string[] ManagementRoles = [SystemAdmin, ClinicAdmin, SchoolAdmin, Manager];

    /// <summary>
    /// Comma-separated admin roles for [Authorize] attribute.
    /// </summary>
    public const string AdminRolesString = $"{SystemAdmin},{ClinicAdmin},{SchoolAdmin}";

    /// <summary>
    /// Comma-separated management roles for [Authorize] attribute.
    /// </summary>
    public const string ManagementRolesString = $"{SystemAdmin},{ClinicAdmin},{SchoolAdmin},{Manager}";
}

/// <summary>
/// Defines authorization policy names used across all layers.
/// </summary>
public static class AuthPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireClinicAdmin = "RequireClinicAdmin";
    public const string RequireSchoolAdmin = "RequireSchoolAdmin";
    public const string RequireManagement = "RequireManagement";
    public const string RequireAuthenticated = "RequireAuthenticated";
}
