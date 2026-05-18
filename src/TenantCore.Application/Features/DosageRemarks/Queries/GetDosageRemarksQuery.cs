using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.DosageRemarks.Queries;

public sealed record GetDosageRemarksQuery(
    Guid ApplicationId,
    int Page,
    int PageSize,
    MedicineFormType? Form) : IRequest<PagedResult<DosageRemarkDto>>;
