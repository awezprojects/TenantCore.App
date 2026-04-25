using MediatR;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Application.Features.MedicineTypes.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Handlers;

public sealed class GetMedicineTypesHandler(IMedicineTypeRepository repository)
    : IRequestHandler<GetMedicineTypesQuery, PagedResult<MedicineTypeDto>>
{
    public async Task<PagedResult<MedicineTypeDto>> Handle(GetMedicineTypesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedAsync(
            request.Page, request.PageSize, request.Search, cancellationToken);

        return new PagedResult<MedicineTypeDto>
        {
            Items = MedicineTypeTranslator.ToDtoList(items).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
