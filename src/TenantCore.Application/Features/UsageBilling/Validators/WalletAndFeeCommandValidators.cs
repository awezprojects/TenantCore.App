using FluentValidation;
using Microsoft.Extensions.Options;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Services;

namespace TenantCore.Application.Features.UsageBilling.Validators;

/// <summary>
/// Bounds the top-up amount from configuration, so a typo cannot create an absurd payment link and a
/// one-rupee top-up cannot be used to brute-force the gateway.
/// </summary>
public sealed class CreateWalletTopUpCommandValidator : AbstractValidator<CreateWalletTopUpCommand>
{
    public CreateWalletTopUpCommandValidator(IOptions<UsageBillingOptions> options)
    {
        var min = options.Value.TopUpMinAmount;
        var max = options.Value.TopUpMaxAmount;

        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.ActingUserId).NotEmpty();

        RuleFor(x => x.Amount)
            .InclusiveBetween(min, max)
            .WithMessage($"A top-up must be between {min:N2} and {max:N2}.");

        RuleFor(x => x.Amount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("The amount can have at most 2 decimal places.");
    }
}

public sealed class CheckWalletTopUpCommandValidator : AbstractValidator<CheckWalletTopUpCommand>
{
    public CheckWalletTopUpCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}

/// <summary>
/// Only identifiers: the amount is resolved server-side, so there is nothing here for the payer to
/// choose.
/// </summary>
public sealed class CreateRegistrationFeePaymentCommandValidator : AbstractValidator<CreateRegistrationFeePaymentCommand>
{
    public CreateRegistrationFeePaymentCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.ActingUserId).NotEmpty();
    }
}

public sealed class CheckRegistrationFeePaymentCommandValidator : AbstractValidator<CheckRegistrationFeePaymentCommand>
{
    public CheckRegistrationFeePaymentCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}
