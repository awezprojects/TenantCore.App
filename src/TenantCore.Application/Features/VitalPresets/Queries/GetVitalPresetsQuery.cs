using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.VitalPresets.Queries;

public sealed record GetVitalPresetsQuery(Guid ApplicationId) : IRequest<IEnumerable<VitalPresetLookupItemDto>>;
