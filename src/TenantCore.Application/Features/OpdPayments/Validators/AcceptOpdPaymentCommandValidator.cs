using FluentValidation;
using TenantCore.Application.Features.OpdPayments.Commands;

namespace TenantCore.Application.Features.OpdPayments.Validators;

public sealed class AcceptOpdPaymentCommandValidator : AbstractValidator<AcceptOpdPaymentCommand>
{
    public AcceptOpdPaymentCommandValidator()
    {
        RuleFor(x => x.Request.OpdRegistrationId).NotEmpty();
        RuleFor(x => x.ReceivedByUserId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
