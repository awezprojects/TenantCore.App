using MediatR;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Application.Features.Prescriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Handlers;

public sealed class GetPrescriptionsHandler(
    IPrescriptionRepository prescriptionRepository,
    IPatientRepository patientRepository)
    : IRequestHandler<GetPrescriptionsQuery, PagedResult<PrescriptionDto>>
{
    public async Task<PagedResult<PrescriptionDto>> Handle(
        GetPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await prescriptionRepository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search,
            request.DoctorUserId, request.PatientId, request.From, request.To,
            cancellationToken);

        var dtos = new List<PrescriptionDto>();
        foreach (var p in items)
        {
            var patient = await patientRepository.GetByIdAsync(p.PatientId, cancellationToken);
            if (patient is not null)
                dtos.Add(PrescriptionTranslator.ToDto(p, patient));
        }

        return new PagedResult<PrescriptionDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
