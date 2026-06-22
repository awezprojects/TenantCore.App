using FluentValidation;
using TenantCore.Application.Features.AmountHandovers.Commands;

namespace TenantCore.Application.Features.AmountHandovers.Validators;

public sealed class AcceptAmountHandoverCommandValidator : AbstractValidator<AcceptAmountHandoverCommand>
{
    public AcceptAmountHandoverCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
