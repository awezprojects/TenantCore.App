using FluentValidation;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Application.Features.Subscriptions.Validators;

public sealed class SubscribeToPlanCommandValidator : AbstractValidator<SubscribeToPlanCommand>
{
    public SubscribeToPlanCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();
        RuleFor(x => x.ActingUserId).NotEmpty();
    }
}
