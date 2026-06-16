using MediatR;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Application.Features.Medicines.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Handlers;

public sealed class GetMedicineAutocompleteHandler(IMedicineRepository repository)
    : IRequestHandler<GetMedicineAutocompleteQuery, IEnumerable<MedicineDto>>
{
    public async Task<IEnumerable<MedicineDto>> Handle(
        GetMedicineAutocompleteQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Min(request.Limit, 50);
        var items = await repository.GetByNamePrefixAsync(request.Name, limit, cancellationToken);
        return MedicineTranslator.ToDtoList(items);
    }
}
