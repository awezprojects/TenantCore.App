using MediatR;
using TenantCore.Application.Features.VitalPresets.Queries;
using TenantCore.Application.Features.VitalPresets.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.VitalPresets.Handlers;

public sealed class GetVitalPresetsHandler(IVitalPresetLookupItemRepository repository)
    : IRequestHandler<GetVitalPresetsQuery, IEnumerable<VitalPresetLookupItemDto>>
{
    public async Task<IEnumerable<VitalPresetLookupItemDto>> Handle(
        GetVitalPresetsQuery request, CancellationToken cancellationToken)
    {
        var items = await repository.GetForApplicationAsync(request.ApplicationId, cancellationToken);
        return VitalPresetLookupItemTranslator.ToDtoList(items);
    }
}
