using MediatR;
using TenantCore.Application.Features.Obstetrics.Queries;
using TenantCore.Application.Features.Obstetrics.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Handlers;

public sealed class GetObstetricDatesHandler(
    IObstetricPrescriptionDataRepository obstetricRepository,
    IPrescriptionRepository prescriptionRepository)
    : IRequestHandler<GetObstetricDatesQuery, ObstetricDatesDto>
{
    public async Task<ObstetricDatesDto> Handle(GetObstetricDatesQuery request, CancellationToken cancellationToken)
    {
        var prescription = await prescriptionRepository.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        var obstetricData = await obstetricRepository.GetByPrescriptionIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ObstetricPrescriptionData), request.PrescriptionId);

        return ObstetricDatesTranslator.ToDto(obstetricData);
    }
}
