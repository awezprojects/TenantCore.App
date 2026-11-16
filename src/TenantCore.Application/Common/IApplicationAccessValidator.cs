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

    /// <summary>
    /// Returns true if the current user holds at least one of <paramref name="roles"/>
    /// specifically for <paramref name="applicationId"/> — the clinic the targeted entity
    /// actually belongs to, not whatever clinic X-Application-Id happens to say.
    ///
    /// Exists because ClinicRoleAuthorizationHandler (the controller-level [Authorize(Policy=...)]
    /// gate) resolves its applicationId from the route's own "applicationId" segment when one
    /// exists, but otherwise falls back to the X-Application-Id header — and routes shaped like
    /// {id:guid}/transfer-bed (the registration/record id, not a clinic id) have no such segment.
    /// For a user who holds roles in more than one clinic, that means the controller-level policy
    /// can pass against clinic A's header while a handler then acts on a clinic B record it fetched
    /// by id — CanAccess alone only proves "linked to clinic B at all," not "holds the right role
    /// there." Call this for any mutation whose authorization must track the record's own clinic
    /// rather than the header, as a second, record-scoped gate underneath the coarser policy.
    /// </summary>
    bool HasRole(Guid applicationId, IEnumerable<string> roles);
}
