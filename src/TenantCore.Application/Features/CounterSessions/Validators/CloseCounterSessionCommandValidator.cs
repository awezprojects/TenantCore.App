using FluentValidation;
using TenantCore.Application.Features.CounterSessions.Commands;

namespace TenantCore.Application.Features.CounterSessions.Validators;

public sealed class CloseCounterSessionCommandValidator : AbstractValidator<CloseCounterSessionCommand>
{
    public CloseCounterSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
