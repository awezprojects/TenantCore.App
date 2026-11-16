using MediatR;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Handlers;

public sealed class UpdateClinicFeatureFlagsHandler(IClinicFeatureFlagsRepository repository)
    : IRequestHandler<UpdateClinicFeatureFlagsCommand, ClinicFeatureFlagsDto>
{
    public async Task<ClinicFeatureFlagsDto> Handle(
        UpdateClinicFeatureFlagsCommand request, CancellationToken cancellationToken)
    {
        var flags = await repository.GetByApplicationAsync(request.ApplicationId, cancellationToken);

        if (flags is null)
        {
            flags = ClinicFeatureFlags.Create(request.ApplicationId, request.PrepaidOpdEnabled, request.BillingEnabled);
            flags.Update(request.PrepaidOpdEnabled, request.BillingEnabled, request.IpdRoomBillingCycle, request.IpdMidStayChargeMode);
            flags.UpdateShiftChargingSettings(
                request.IpdShiftGraceWindowMinutes, request.IpdShiftGraceRule, request.IpdManualShiftChargeMode, request.IpdFullDayShiftRateRule);
            flags.UpdateCancelWindow(request.IpdCancelWindowMinutes);
            await repository.AddAsync(flags, cancellationToken);
        }
        else
        {
            flags.Update(request.PrepaidOpdEnabled, request.BillingEnabled, request.IpdRoomBillingCycle, request.IpdMidStayChargeMode);
            flags.UpdateShiftChargingSettings(
                request.IpdShiftGraceWindowMinutes, request.IpdShiftGraceRule, request.IpdManualShiftChargeMode, request.IpdFullDayShiftRateRule);
            flags.UpdateCancelWindow(request.IpdCancelWindowMinutes);
            repository.Update(flags);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ClinicFeatureFlagsTranslator.ToDto(flags);
    }
}
