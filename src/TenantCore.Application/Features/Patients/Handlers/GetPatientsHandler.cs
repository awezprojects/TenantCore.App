using MediatR;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Application.Features.Patients.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class GetPatientsHandler(
    IPatientRepository repository,
    IPregnancyTenureRepository tenureRepository,
    IObstetricPrescriptionDataRepository obstetricRepository)
    : IRequestHandler<GetPatientsQuery, PagedResult<PatientDto>>
{
    public async Task<PagedResult<PatientDto>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search, cancellationToken);

        var patientIds = items.Select(p => p.Id).ToList();

        Dictionary<Guid, bool> tenureInfo;
        HashSet<Guid> obstetricLmpIds;

        if (patientIds.Count > 0)
        {
            tenureInfo = await tenureRepository.GetTenureInfoForPatientsAsync(patientIds, request.ApplicationId, cancellationToken);
            obstetricLmpIds = await obstetricRepository.GetPatientIdsWithLmpAsync(patientIds, request.ApplicationId, cancellationToken);
        }
        else
        {
            tenureInfo = new Dictionary<Guid, bool>();
            obstetricLmpIds = [];
        }

        return new PagedResult<PatientDto>
        {
            Items = items.Select(p =>
            {
                var hasTenure = tenureInfo.ContainsKey(p.Id);
                var hasLmp = hasTenure || obstetricLmpIds.Contains(p.Id);
                var hasActive = hasTenure && tenureInfo[p.Id];
                return PatientTranslator.ToDto(p, request.ShowFullAadhaar, hasLmp, hasActive);
            }).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
