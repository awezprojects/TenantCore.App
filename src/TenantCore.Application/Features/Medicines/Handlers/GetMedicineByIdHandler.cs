using MediatR;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Application.Features.Medicines.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Handlers;

public sealed class GetMedicineByIdHandler(IMedicineRepository repository)
    : IRequestHandler<GetMedicineByIdQuery, MedicineDto>
{
    public async Task<MedicineDto> Handle(GetMedicineByIdQuery request, CancellationToken cancellationToken)
    {
        var medicine = await repository.GetByIdWithTypeAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Medicine), request.Id);

        return MedicineTranslator.ToDto(medicine);
    }
}
