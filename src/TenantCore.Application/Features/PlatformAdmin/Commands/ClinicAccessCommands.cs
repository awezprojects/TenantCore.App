using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>
/// Internal — sent by the Admin portal via the InternalService scheme. ApplicationId always comes
/// from the route, never from the body.
///
/// Suspension blocks every guarded clinic-scoped request (403) but does not pause the subscription
/// clock and does not touch the users' Auth accounts.
/// </summary>
public sealed record SuspendClinicCommand(
    Guid ApplicationId, string Message, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Suspended";

    // Identifiers only — the admin's message is free text and never reaches the log (ADR-011).
    public string ActionLogContext => $"applicationId={ApplicationId}; adminUserId={AdminUserId}";
}

/// <summary>Internal — lifts a suspension. Access resumes immediately if the clinic has a current term.</summary>
public sealed record ReactivateClinicCommand(
    Guid ApplicationId, Guid AdminUserId, string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Reactivated";

    public string ActionLogContext => $"applicationId={ApplicationId}; adminUserId={AdminUserId}";
}
