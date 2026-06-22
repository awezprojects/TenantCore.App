using FluentValidation;
using TenantCore.Application.Features.CounterSessions.Commands;

namespace TenantCore.Application.Features.CounterSessions.Validators;

public sealed class OpenCounterSessionCommandValidator : AbstractValidator<OpenCounterSessionCommand>
{
    public OpenCounterSessionCommandValidator()
    {
        RuleFor(x => x.OpenedByUserId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
