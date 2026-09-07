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
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-]+$").WithMessage("Phone number format is invalid.");
        RuleFor(x => x.AadhaarNumber)
            .Length(12)
            .Matches(@"^[0-9]{12}$").WithMessage("Aadhaar number must be exactly 12 digits.")
            .When(x => !string.IsNullOrEmpty(x.AadhaarNumber));
        RuleFor(x => x.Email)
            .EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
