using FluentValidation;
using TenantCore.Application.Features.AmountHandovers.Commands;

namespace TenantCore.Application.Features.AmountHandovers.Validators;

public sealed class CreateAmountHandoverCommandValidator : AbstractValidator<CreateAmountHandoverCommand>
{
    public CreateAmountHandoverCommandValidator()
    {
        RuleFor(x => x.Request.CounterSessionId).NotEmpty();
        RuleFor(x => x.HandedOverByUserId).NotEmpty();
        RuleFor(x => x.Request.HandedOverToUserId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => x.Request.Notes is not null);
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
