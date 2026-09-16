using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Patients.Commands;

namespace TenantCore.Application.Features.Patients.Validators;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty().WithMessage("A clinic must be selected before registering a patient.");
        RuleFor(x => x.DateOfBirth!.Value)
            .Must(DateValidationRules.IsValidDateOfBirth)
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must not be in the future and not more than 120 years ago.");
        RuleFor(x => x.FirstName)
            .NotEmpty().MaximumLength(100)
            .Must(NameValidationRules.IsValidName).WithMessage("First name may only contain letters, spaces, hyphens, and apostrophes.");
        RuleFor(x => x.LastName)
            .NotEmpty().MaximumLength(100)
            .Must(NameValidationRules.IsValidName).WithMessage("Last name may only contain letters, spaces, hyphens, and apostrophes.");
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(PhoneValidationRules.IsValidPhoneNumber).WithMessage("Phone number must be exactly 10 digits.");
        RuleFor(x => x.EmergencyContactPhone)
            .Must(PhoneValidationRules.IsValidPhoneNumber)
            .When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone))
            .WithMessage("Emergency contact phone must be exactly 10 digits.");
        RuleFor(x => x.EmergencyContactName)
            .Must(NameValidationRules.IsValidName)
            .When(x => !string.IsNullOrEmpty(x.EmergencyContactName))
            .WithMessage("Emergency contact name may only contain letters, spaces, hyphens, and apostrophes.");
        RuleFor(x => x.Address)
            .Must(HtmlSanitizationRules.HasNoHtmlTags)
            .When(x => !string.IsNullOrEmpty(x.Address))
            .WithMessage("Address must not contain '<' or '>' characters.");
        RuleFor(x => x.AadhaarNumber)
            .Length(12)
            .Matches(@"^[0-9]{12}$").WithMessage("Aadhaar number must be exactly 12 digits.")
            .When(x => !string.IsNullOrEmpty(x.AadhaarNumber));
        RuleFor(x => x.Email)
            .EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
