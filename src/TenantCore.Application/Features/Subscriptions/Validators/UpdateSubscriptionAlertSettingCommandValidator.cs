using FluentValidation;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Application.Features.Subscriptions.Validators;

public sealed class UpdateSubscriptionAlertSettingCommandValidator : AbstractValidator<UpdateSubscriptionAlertSettingCommand>
{
    public UpdateSubscriptionAlertSettingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Headline).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.BodyMessage).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
