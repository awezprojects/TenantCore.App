using MediatR;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Application.Features.Medicines.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Handlers;

public sealed class GetMedicinesHandler(IMedicineRepository repository)
    : IRequestHandler<GetMedicinesQuery, PagedResult<MedicineDto>>
{
    public async Task<PagedResult<MedicineDto>> Handle(GetMedicinesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedAsync(
            request.Page, request.PageSize, request.Search,
            request.BrandName, request.GenericName, request.MedicineTypeId,
            request.IsGeneric, includeInactive: false, cancellationToken);

        return new PagedResult<MedicineDto>
        {
            Items = MedicineTranslator.ToDtoList(items).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
