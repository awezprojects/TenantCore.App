using FluentValidation;
using TenantCore.Application.Features.OpdPayments.Commands;

namespace TenantCore.Application.Features.OpdPayments.Validators;

public sealed class ApplyOpdDiscountCommandValidator : AbstractValidator<ApplyOpdDiscountCommand>
{
    public ApplyOpdDiscountCommandValidator()
    {
        RuleFor(x => x.Request.OpdRegistrationId).NotEmpty();
        RuleFor(x => x.Request.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
