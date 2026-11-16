using FluentValidation;
using TenantCore.Application.Features.IpdPayments.Commands;

namespace TenantCore.Application.Features.IpdPayments.Validators;

public sealed class CollectIpdBalanceCommandValidator : AbstractValidator<CollectIpdBalanceCommand>
{
    public CollectIpdBalanceCommandValidator()
    {
        RuleFor(x => x.IpdRegistrationId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.ReceivedByUserId).NotEmpty();
    }
}
