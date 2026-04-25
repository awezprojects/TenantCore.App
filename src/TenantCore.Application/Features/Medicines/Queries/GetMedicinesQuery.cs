using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Medicines.Queries;

public sealed record GetMedicinesQuery(
    int Page,
    int PageSize,
    string? Search,
    string? BrandName,
    string? GenericName,
    Guid? MedicineTypeId,
    bool? IsGeneric) : IRequest<PagedResult<MedicineDto>>;
