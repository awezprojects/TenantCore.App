using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>
/// Internal — sent by the Admin portal behind the InternalService scheme. ApplicationId always comes
/// from the route, never from the body. Both commands return the clinic's status AFTER the change, so
/// the portal never has to re-read to find out what happened.
/// </summary>
public sealed record SeedDummyDataCommand(Guid ApplicationId, Guid AdminUserId, string AdminEmail)
    : IRequest<DummyDataStatusDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Dummy Data Seeded";

    // Identifiers only. Never a patient name, phone number or any other personal data (ADR-011).
    public string? ActionLogContext => $"applicationId={ApplicationId}; adminUserId={AdminUserId}";
}

/// <summary>
/// Internal — cascade-deletes every dummy record this clinic holds. 409 when there is nothing to purge.
/// </summary>
public sealed record PurgeDummyDataCommand(Guid ApplicationId, Guid AdminUserId, string AdminEmail)
    : IRequest<DummyDataStatusDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Dummy Data Purged";

    public string? ActionLogContext => $"applicationId={ApplicationId}; adminUserId={AdminUserId}";
}
