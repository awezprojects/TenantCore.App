using FluentValidation;
using TenantCore.Application.Features.PlatformAdmin.Models;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

/// <summary>
/// Shared by the grant and payment-link validators. Name is already defaulted to the clinic name by
/// the translator, so an empty one here means the portal sent neither — which would silently drop
/// the email at the notification consumer.
/// </summary>
public sealed class ClinicContactValidator : AbstractValidator<ClinicContact>
{
    public ClinicContactValidator()
    {
        RuleFor(x => x.ClinicName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}
