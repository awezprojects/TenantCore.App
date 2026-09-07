using FluentValidation;
using TenantCore.Application.Features.AmountHandovers.Commands;

namespace TenantCore.Application.Features.AmountHandovers.Validators;

public sealed class CollectAmountHandoverCommandValidator : AbstractValidator<CollectAmountHandoverCommand>
{
    public CollectAmountHandoverCommandValidator()
    {
        RuleFor(x => x.Request.CounterSessionId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Notes).MaximumLength(500).When(x => x.Request.Notes is not null);
        RuleFor(x => x.CollectedByUserId).NotEmpty();
        RuleFor(x => x.CollectedByRoleName).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
