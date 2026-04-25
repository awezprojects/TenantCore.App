using MediatR;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Application.Features.MedicineTypes.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Handlers;

public sealed class GetMedicineTypeByIdHandler(IMedicineTypeRepository repository)
    : IRequestHandler<GetMedicineTypeByIdQuery, MedicineTypeDto>
{
    public async Task<MedicineTypeDto> Handle(GetMedicineTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var medicineType = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineType), request.Id);

        return MedicineTypeTranslator.ToDto(medicineType);
    }
}
