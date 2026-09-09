using MediatR;
using TenantCore.Application.Features.VitalPresets.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.VitalPresets.Handlers;

public sealed class DeleteVitalPresetHandler(IVitalPresetLookupItemRepository repository)
    : IRequestHandler<DeleteVitalPresetCommand>
{
    public async Task Handle(DeleteVitalPresetCommand request, CancellationToken cancellationToken)
    {
        var preset = await repository.GetByIdAsync(request.Id, cancellationToken);

        // A global default (ApplicationId == null) can never match request.ApplicationId,
        // so this also protects system defaults from being deleted by any clinic.
        if (preset is null || preset.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(VitalPresetLookupItem), request.Id);

        repository.Delete(preset);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
