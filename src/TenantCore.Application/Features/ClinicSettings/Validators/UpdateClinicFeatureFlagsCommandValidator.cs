using FluentValidation;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.ClinicSettings.Validators;

public sealed class UpdateClinicFeatureFlagsCommandValidator : AbstractValidator<UpdateClinicFeatureFlagsCommand>
{
    public UpdateClinicFeatureFlagsCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.IpdRoomBillingCycle)
            .Must(v => Enum.IsDefined(typeof(RoomBillingCycle), v!.Value)).WithMessage("Invalid room billing cycle.")
            .When(x => x.IpdRoomBillingCycle.HasValue);
        RuleFor(x => x.IpdMidStayChargeMode)
            .Must(v => Enum.IsDefined(typeof(MidStayBedChangeChargeMode), v!.Value)).WithMessage("Invalid mid-stay charge mode.")
            .When(x => x.IpdMidStayChargeMode.HasValue);

        // OBS-23 (14-Nov QA): had no upper bound at all — 100000 minutes (~69 days) was accepted.
        // 1440 (24 hours) is already generous for a "quick shift" grace window; beyond that it
        // stops being a grace exception and starts silently overriding the mid-stay charge mode
        // for the admission's entire stay.
        RuleFor(x => x.IpdShiftGraceWindowMinutes).InclusiveBetween(0, 1440)
            .WithMessage("The grace window must be between 0 and 1440 minutes (24 hours).");
        RuleFor(x => x.IpdShiftGraceRule)
            .Must(v => Enum.IsDefined(typeof(IpdShiftGraceRule), v!.Value)).WithMessage("Invalid shift grace rule.")
            .When(x => x.IpdShiftGraceRule.HasValue);
        RuleFor(x => x)
            .Must(x => x.IpdShiftGraceWindowMinutes == 0 || x.IpdShiftGraceRule.HasValue)
            .WithMessage("Select a grace rule once the grace window is turned on.")
            .WithName("IpdShiftGraceRule");
        RuleFor(x => x.IpdManualShiftChargeMode)
            .Must(v => Enum.IsDefined(typeof(IpdManualShiftChargeMode), v)).WithMessage("Invalid manual shift charge mode.");
        RuleFor(x => x.IpdFullDayShiftRateRule)
            .Must(v => Enum.IsDefined(typeof(IpdFullDayShiftRateRule), v)).WithMessage("Invalid full-day shift rate rule.");

        // DEF-IPD-26 (15-Nov): 0 = unlimited (today's behavior). 1440 (24 hours) is generous for
        // "catch an admission mistake quickly" — same cap as the shift grace window, for the same
        // reason: beyond it this isn't really a quick-undo window anymore.
        RuleFor(x => x.IpdCancelWindowMinutes).InclusiveBetween(0, 1440)
            .WithMessage("The cancellation window must be between 0 and 1440 minutes (24 hours).");
    }
}
