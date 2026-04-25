using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Handlers;

public sealed class CreateMedicineHandler(
    IMedicineRepository repository,
    ILogger<CreateMedicineHandler> logger)
    : IRequestHandler<CreateMedicineCommand, MedicineDto>
{
    public async Task<MedicineDto> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating medicine {Name}", request.Name);

        var similar = await repository.FindSimilarAsync(
            request.Name, request.GenericName, request.BrandName, excludeId: null, cancellationToken);

        var conflicts = similar.ToList();
        if (conflicts.Count > 0)
        {
            var names = string.Join(", ", conflicts.Select(m => $"'{m.Name}'"));
            throw new InvalidOperationException(
                $"Similar medicine(s) already exist: {names}. Please review before adding a duplicate.");
        }

        var medicine = Medicine.Create(
            request.Name, request.GenericName, request.BrandName, request.Description,
            request.Composition, request.Composition2, request.Dosage, request.Form,
            request.Manufacturer, request.IsGeneric, request.PackSize, request.Uses,
            request.SideEffects, request.Contraindications, request.Storage, request.MedicineTypeId);

        await repository.AddAsync(medicine, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineTranslator.ToDto(medicine);
    }
}
