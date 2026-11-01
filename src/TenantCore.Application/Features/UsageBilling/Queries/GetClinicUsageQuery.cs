using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Queries;

/// <summary>
/// Metered usage over one range, with per-item totals, a daily series and the newest rows. The range
/// is chosen by the caller <b>once</b>; changing the tab, sorting, filtering or exporting afterwards
/// must be served from what this one call returned (workspace rule 7).
/// </summary>
public sealed record GetClinicUsageQuery(Guid ApplicationId, DateTime From, DateTime To)
    : IRequest<ClinicUsageDto>;
