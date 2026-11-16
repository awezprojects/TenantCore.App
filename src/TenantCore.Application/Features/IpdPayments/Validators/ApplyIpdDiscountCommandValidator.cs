using FluentValidation;
using TenantCore.Application.Features.IpdPayments.Commands;

namespace TenantCore.Application.Features.IpdPayments.Validators;

public sealed class ApplyIpdDiscountCommandValidator : AbstractValidator<ApplyIpdDiscountCommand>
{
    public ApplyIpdDiscountCommandValidator()
    {
        RuleFor(x => x.IpdRegistrationId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.DiscountedByUserId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
    }
}
