using FluentValidation;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

/// <summary>
/// Internal-admin billing commands. The admin's identity is checked because the audit trail is what
/// holds this role to account — an unnamed actor defeats the whole point of the trail.
/// </summary>
public sealed class SetClinicBillingCommandValidator : AbstractValidator<SetClinicBillingCommand>
{
    public SetClinicBillingCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.BillingMode).IsInEnum();

        // Every billing change carries a reason — it is the audit trail's whole point.
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(3).MaximumLength(500);

        RuleFor(x => x.RegistrationFee)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.RegistrationFee.HasValue)
            .WithMessage("The registration fee cannot be negative.");

        RuleFor(x => x.RegistrationFee)
            .Must(f => f is null || decimal.Round(f.Value, 2) == f.Value)
            .WithMessage("The registration fee can have at most 2 decimal places.");

        RuleFor(x => x.FreeTestDays)
            .InclusiveBetween(30, 90)
            .When(x => x.FreeTestDays.HasValue)
            .WithMessage("Free test days must be between 30 and 90.");
    }
}

public sealed class SetClinicUsageRatesCommandValidator : AbstractValidator<SetClinicUsageRatesCommand>
{
    public SetClinicUsageRatesCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Reason).MaximumLength(500);

        RuleFor(x => x.Rates)
            .NotEmpty()
            .WithMessage("Set at least one rate.");

        RuleFor(x => x.Rates)
            .Must(rates => rates.Select(r => r.BillingItem).Distinct().Count() == rates.Count)
            .WithMessage("Each operation can appear only once.");

        RuleForEach(x => x.Rates).ChildRules(rate =>
        {
            rate.RuleFor(r => r.BillingItem).IsInEnum();

            rate.RuleFor(r => r.Rate)
                .GreaterThanOrEqualTo(0m)
                .LessThanOrEqualTo(100000m)
                .Must(v => decimal.Round(v, 4) == v)
                .WithMessage("A rate can have at most 4 decimal places.");
        });
    }
}

public sealed class SendClinicWalletTopUpCommandValidator : AbstractValidator<SendClinicWalletTopUpCommand>
{
    public SendClinicWalletTopUpCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Amount).Must(a => decimal.Round(a, 2) == a)
            .WithMessage("The amount can have at most 2 decimal places.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreditClinicWalletCommandValidator : AbstractValidator<CreditClinicWalletCommand>
{
    public CreditClinicWalletCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        // Guid.Empty is the accepted sentinel for system-automated credits (e.g. deferred wallet
        // credit released by RecordRegistrationFeeTaskHandler). AdminEmail still enforces identity.
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Amount).Must(a => decimal.Round(a, 2) == a)
            .WithMessage("The amount can have at most 2 decimal places.");

        // The idempotency key is the only thing standing between a retry and free money, so it is
        // mandatory rather than optional.
        RuleFor(x => x.ReferenceId).NotEmpty()
            .WithMessage("An idempotency reference is required so a retried credit cannot pay twice.");

        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class WaiveRegistrationFeeCommandValidator : AbstractValidator<WaiveRegistrationFeeCommand>
{
    public WaiveRegistrationFeeCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class AdminCheckRegistrationFeePaymentCommandValidator : AbstractValidator<AdminCheckRegistrationFeePaymentCommand>
{
    public AdminCheckRegistrationFeePaymentCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
    }
}
