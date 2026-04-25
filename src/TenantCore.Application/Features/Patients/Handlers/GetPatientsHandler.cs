using MediatR;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class GetPatientsHandler(IPatientRepository repository)
    : IRequestHandler<GetPatientsQuery, PagedResult<PatientDto>>
{
    public async Task<PagedResult<PatientDto>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search, cancellationToken);

        return new PagedResult<PatientDto>
        {
            Items = items.Select(p => PatientTranslator.ToDto(p, request.ShowFullAadhaar)).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
