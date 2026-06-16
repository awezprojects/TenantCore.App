using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Handlers;

public sealed class SetObstetricEddByUsgHandler(
    IObstetricPrescriptionDataRepository obstetricRepository,
    IPrescriptionRepository prescriptionRepository,
    ILogger<SetObstetricEddByUsgHandler> logger)
    : IRequestHandler<SetObstetricEddByUsgCommand, ObstetricDatesDto>
{
    public async Task<ObstetricDatesDto> Handle(SetObstetricEddByUsgCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting EDD by USG for prescription {PrescriptionId}", request.PrescriptionId);

        var prescription = await prescriptionRepository.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        var obstetricData = await obstetricRepository.GetByPrescriptionIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ObstetricPrescriptionData), request.PrescriptionId);

        obstetricData.SetEddByUsg(request.Request.EddByUsg);
        obstetricRepository.Update(obstetricData);
        await obstetricRepository.SaveChangesAsync(cancellationToken);

        return ObstetricDatesTranslator.ToDto(obstetricData);
    }
}
