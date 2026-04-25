using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Handlers;

public sealed class CreateMedicineTypeHandler(
    IMedicineTypeRepository repository,
    ILogger<CreateMedicineTypeHandler> logger)
    : IRequestHandler<CreateMedicineTypeCommand, MedicineTypeDto>
{
    public async Task<MedicineTypeDto> Handle(CreateMedicineTypeCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating medicine type {Name}", request.Name);

        var existing = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A medicine type with the name '{request.Name}' already exists.");

        var medicineType = MedicineType.Create(request.Name, request.Description);
        await repository.AddAsync(medicineType, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineTypeTranslator.ToDto(medicineType);
    }
}
