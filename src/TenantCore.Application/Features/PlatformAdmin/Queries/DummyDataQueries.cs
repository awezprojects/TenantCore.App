using MediatR;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Features.PlatformAdmin.Queries;

/// <summary>
/// Internal — the Admin portal's Test Data page. Read-only, and silent in the action log (queries
/// only appear in the request log when they fail or are slow). The DummyData:Enabled guard lives on
/// InternalDummyDataController.
/// </summary>
public sealed record GetDummyDataStatusQuery(Guid ApplicationId) : IRequest<DummyDataStatusDto>;
