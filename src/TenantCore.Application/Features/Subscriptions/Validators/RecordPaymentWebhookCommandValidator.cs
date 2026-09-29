using FluentValidation;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Application.Features.Subscriptions.Validators;

public sealed class RecordPaymentWebhookCommandValidator : AbstractValidator<RecordPaymentWebhookCommand>
{
    public RecordPaymentWebhookCommandValidator()
    {
        RuleFor(x => x.RawBody).NotEmpty().MaximumLength(65536);
        RuleFor(x => x.Signature).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EventId).NotEmpty().MaximumLength(100);
    }
}
