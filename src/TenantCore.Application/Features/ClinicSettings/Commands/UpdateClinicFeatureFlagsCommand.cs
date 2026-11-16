using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.ClinicSettings.Commands;

public sealed record UpdateClinicFeatureFlagsCommand(
    Guid ApplicationId,
    bool PrepaidOpdEnabled,
    bool BillingEnabled,
    RoomBillingCycle? IpdRoomBillingCycle,
    MidStayBedChangeChargeMode? IpdMidStayChargeMode,
    int IpdShiftGraceWindowMinutes = 0,
    IpdShiftGraceRule? IpdShiftGraceRule = null,
    IpdManualShiftChargeMode IpdManualShiftChargeMode = IpdManualShiftChargeMode.Off,
    IpdFullDayShiftRateRule IpdFullDayShiftRateRule = IpdFullDayShiftRateRule.DestinationRoomRate,
    int IpdCancelWindowMinutes = 0) : IRequest<ClinicFeatureFlagsDto>;
