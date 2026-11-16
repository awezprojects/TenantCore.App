using FluentValidation;
using TenantCore.Application.Features.IpdPayments.Commands;

namespace TenantCore.Application.Features.IpdPayments.Validators;

public sealed class ProcessIpdRefundCommandValidator : AbstractValidator<ProcessIpdRefundCommand>
{
    public ProcessIpdRefundCommandValidator()
    {
        RuleFor(x => x.IpdRegistrationId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RefundedByUserId).NotEmpty();
    }
}
