using FluentValidation;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Application.Features.Subscriptions.Validators;

public sealed class CheckSubscriptionPaymentCommandValidator : AbstractValidator<CheckSubscriptionPaymentCommand>
{
    public CheckSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.ActingUserId).NotEmpty();
    }
}
