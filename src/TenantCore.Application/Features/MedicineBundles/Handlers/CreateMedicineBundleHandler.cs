using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.MedicineBundles.Commands;
using TenantCore.Application.Features.MedicineBundles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Handlers;

public sealed class CreateMedicineBundleHandler(
    IMedicineBundleRepository repository,
    ILogger<CreateMedicineBundleHandler> logger)
    : IRequestHandler<CreateMedicineBundleCommand, MedicineBundleDto>
{
    public async Task<MedicineBundleDto> Handle(CreateMedicineBundleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating medicine bundle '{Name}' for application {ApplicationId}",
            request.Request.Name, request.ApplicationId);

        var items = request.Request.Items.Select((dto, index) => MedicineBundleItem.Create(
            Guid.Empty,
            dto.MedicineId,
            dto.MedicineName,
            dto.GenericName,
            dto.MedicineForm,
            dto.Strength,
            dto.DosageUnit,
            dto.DosageMorning,
            dto.DosageAfternoon,
            dto.DosageEvening,
            dto.DosageNight,
            dto.DurationDays,
            CalculateQuantity(dto),
            dto.Frequency,
            dto.Timing,
            dto.Instructions,
            index)).ToList();

        var bundle = MedicineBundle.Create(
            request.ApplicationId,
            request.Request.Name,
            request.Request.DurationDays,
            request.Request.Notes,
            request.Request.DoctorUserId,
            request.Request.DoctorName,
            items);

        await repository.AddAsync(bundle, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MedicineBundleTranslator.ToDto(bundle);
    }

    private static decimal CalculateQuantity(CreateMedicineBundleItemDto dto)
    {
        var dailyDose = (dto.DosageMorning ?? 0) + (dto.DosageAfternoon ?? 0)
                      + (dto.DosageEvening ?? 0) + (dto.DosageNight ?? 0);
        return dailyDose * dto.DurationDays;
    }
}
