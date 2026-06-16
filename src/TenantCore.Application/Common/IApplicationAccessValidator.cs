namespace TenantCore.Application.Common;

/// <summary>
/// Validates whether the currently authenticated user is authorised to access
/// data belonging to a given clinic application.
/// </summary>
public interface IApplicationAccessValidator
{
    /// <summary>
    /// Returns true if the current user's JWT contains <paramref name="applicationId"/>
    /// in its app_ids claims, meaning the user is linked to that clinic.
    /// </summary>
    bool CanAccess(Guid applicationId);
}
