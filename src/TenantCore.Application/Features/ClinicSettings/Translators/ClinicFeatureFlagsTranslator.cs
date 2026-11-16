using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.ClinicSettings.Translators;

public static class ClinicFeatureFlagsTranslator
{
    public static ClinicFeatureFlagsDto ToDto(ClinicFeatureFlags entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        PrepaidOpdEnabled = entity.PrepaidOpdEnabled,
        BillingEnabled = entity.BillingEnabled,
        IpdRoomBillingCycle = entity.IpdRoomBillingCycle,
        IpdMidStayChargeMode = entity.IpdMidStayChargeMode,
        IpdShiftGraceWindowMinutes = entity.IpdShiftGraceWindowMinutes,
        IpdShiftGraceRule = entity.IpdShiftGraceRule,
        IpdManualShiftChargeMode = entity.IpdManualShiftChargeMode,
        IpdFullDayShiftRateRule = entity.IpdFullDayShiftRateRule,
        IpdCancelWindowMinutes = entity.IpdCancelWindowMinutes,
        UpdatedAt = entity.UpdatedAt
    };
}
