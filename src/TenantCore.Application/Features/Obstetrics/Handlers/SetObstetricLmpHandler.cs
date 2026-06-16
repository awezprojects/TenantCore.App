using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Obstetrics.Commands;
using TenantCore.Application.Features.Obstetrics.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Obstetrics.Handlers;

public sealed class SetObstetricLmpHandler(
    IObstetricPrescriptionDataRepository obstetricRepository,
    IPrescriptionRepository prescriptionRepository,
    IPregnancyTenureRepository tenureRepository,
    ILogger<SetObstetricLmpHandler> logger)
    : IRequestHandler<SetObstetricLmpCommand, ObstetricDatesDto>
{
    public async Task<ObstetricDatesDto> Handle(SetObstetricLmpCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting LMP for prescription {PrescriptionId}", request.PrescriptionId);

        var prescription = await prescriptionRepository.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        if (prescription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(Prescription), request.PrescriptionId);

        var existingTenure = await tenureRepository.GetActiveForPatientAsync(prescription.PatientId, request.ApplicationId, cancellationToken);
        if (existingTenure is not null)
        {
            var effectiveEdd = existingTenure.EddByUsg ?? existingTenure.EddByLmp;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (effectiveEdd < today)
                throw new InvalidOperationException(
                    "This patient has an overdue active pregnancy tenure. Close the previous tenure before setting a new LMP.");
        }

        var obstetricData = await obstetricRepository.GetByPrescriptionIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ObstetricPrescriptionData), request.PrescriptionId);

        obstetricData.SetLmp(request.Request.Lmp);
        obstetricRepository.Update(obstetricData);
        await obstetricRepository.SaveChangesAsync(cancellationToken);

        var lmp = request.Request.Lmp;
        var eddByLmp = lmp.AddDays(280);

        if (existingTenure is not null)
        {
            existingTenure.Lmp = lmp;
            existingTenure.EddByLmp = eddByLmp;
            tenureRepository.Update(existingTenure);
        }
        else
        {
            var tenure = new PregnancyTenure
            {
                PatientId = prescription.PatientId,
                ApplicationId = request.ApplicationId,
                Lmp = lmp,
                EddByLmp = eddByLmp,
                Status = PregnancyTenureStatus.Active
            };
            await tenureRepository.AddAsync(tenure, cancellationToken);
        }

        await tenureRepository.SaveChangesAsync(cancellationToken);

        return ObstetricDatesTranslator.ToDto(obstetricData);
    }
}
