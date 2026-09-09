using MediatR;
using TenantCore.Application.Features.VitalPresets.Commands;
using TenantCore.Application.Features.VitalPresets.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.VitalPresets.Handlers;

public sealed class AddVitalPresetHandler(IVitalPresetLookupItemRepository repository)
    : IRequestHandler<AddVitalPresetCommand, VitalPresetLookupItemDto>
{
    public async Task<VitalPresetLookupItemDto> Handle(
        AddVitalPresetCommand request, CancellationToken cancellationToken)
    {
        var value = request.Value.Trim();

        // Already exists (globally, or already added by this clinic) — reuse it instead
        // of creating a duplicate row.
        var existing = await repository.FindAsync(request.VitalField, request.ApplicationId, value, cancellationToken);
        if (existing is not null)
            return VitalPresetLookupItemTranslator.ToDto(existing);

        var item = VitalPresetLookupItem.CreateForClinic(request.ApplicationId, request.VitalField, value);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return VitalPresetLookupItemTranslator.ToDto(item);
    }
}
