using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Handlers;

public sealed class UpdateMedicineTypeHandler(
    IMedicineTypeRepository repository,
    ILogger<UpdateMedicineTypeHandler> logger)
    : IRequestHandler<UpdateMedicineTypeCommand, MedicineTypeDto>
{
    public async Task<MedicineTypeDto> Handle(UpdateMedicineTypeCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating medicine type {Id}", request.Id);

        var medicineType = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineType), request.Id);

        var duplicate = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (duplicate is not null && duplicate.Id != request.Id)
            throw new InvalidOperationException($"A medicine type with the name '{request.Name}' already exists.");

        medicineType.Update(request.Name, request.Description, request.IsActive);
        repository.Update(medicineType);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineTypeTranslator.ToDto(medicineType);
    }
}
