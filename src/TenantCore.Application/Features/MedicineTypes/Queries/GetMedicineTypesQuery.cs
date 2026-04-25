using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineTypes.Queries;

public sealed record GetMedicineTypesQuery(int Page, int PageSize, string? Search) : IRequest<PagedResult<MedicineTypeDto>>;
