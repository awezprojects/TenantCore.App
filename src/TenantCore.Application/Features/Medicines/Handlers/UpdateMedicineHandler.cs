using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Handlers;

public sealed class UpdateMedicineHandler(
    IMedicineRepository repository,
    ILogger<UpdateMedicineHandler> logger)
    : IRequestHandler<UpdateMedicineCommand, MedicineDto>
{
    public async Task<MedicineDto> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating medicine {Id}", request.Id);

        var medicine = await repository.GetByIdWithTypeAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Medicine), request.Id);

        var similar = await repository.FindSimilarAsync(
            request.Name, request.GenericName, request.BrandName, excludeId: request.Id, cancellationToken);

        var conflicts = similar.ToList();
        if (conflicts.Count > 0)
        {
            var names = string.Join(", ", conflicts.Select(m => $"'{m.Name}'"));
            throw new InvalidOperationException(
                $"Similar medicine(s) already exist: {names}. Please review before saving changes.");
        }

        medicine.Update(
            request.Name, request.GenericName, request.BrandName, request.Description,
            request.Composition, request.Composition2, request.Dosage, request.Form,
            request.Manufacturer, request.IsGeneric, request.PackSize, request.Uses,
            request.SideEffects, request.Contraindications, request.Storage,
            request.IsActive, request.MedicineTypeId);

        repository.Update(medicine);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineTranslator.ToDto(medicine);
    }
}
