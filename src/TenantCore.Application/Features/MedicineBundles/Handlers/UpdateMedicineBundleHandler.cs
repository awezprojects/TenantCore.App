using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.MedicineBundles.Commands;
using TenantCore.Application.Features.MedicineBundles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Handlers;

public sealed class UpdateMedicineBundleHandler(
    IMedicineBundleRepository repository,
    ILogger<UpdateMedicineBundleHandler> logger)
    : IRequestHandler<UpdateMedicineBundleCommand, MedicineBundleDto>
{
    public async Task<MedicineBundleDto> Handle(UpdateMedicineBundleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating medicine bundle {Id}", request.Id);

        var bundle = await repository.GetByIdWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineBundle), request.Id);

        if (bundle.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(MedicineBundle), request.Id);

        var items = request.Request.Items.Select((dto, index) => MedicineBundleItem.Create(
            bundle.Id,
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

        bundle.Update(request.Request.Name, request.Request.DurationDays, request.Request.Notes, items);
        repository.Update(bundle);
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
