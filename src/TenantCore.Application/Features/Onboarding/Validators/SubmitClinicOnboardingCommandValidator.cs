using FluentValidation;
using TenantCore.Application.Features.Onboarding.Commands;

namespace TenantCore.Application.Features.Onboarding.Validators;

public sealed class SubmitClinicOnboardingCommandValidator : AbstractValidator<SubmitClinicOnboardingCommand>
{
    public SubmitClinicOnboardingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RequesterName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RequesterEmail).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.Request.ClinicName)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(200).WithMessage("Clinic name must not exceed 200 characters.")
            .Matches(@"^[^<>]*$").WithMessage("Clinic name must not contain '<' or '>' characters.");
        RuleFor(x => x.Request.PreferredClinicCode)
            .NotEmpty().WithMessage("Clinic code is required.")
            .Length(3, 20).WithMessage("Clinic code must be between 3 and 20 characters.")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Clinic code must contain only letters, digits and hyphens.");
        RuleFor(x => x.Request.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
        RuleFor(x => x.Request.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");
        RuleFor(x => x.Request.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State must not exceed 100 characters.");
        RuleFor(x => x.Request.Pincode)
            .NotEmpty().WithMessage("Pincode is required.")
            .Matches("^[0-9]{6}$").WithMessage("Pincode must be exactly 6 digits.");
        RuleFor(x => x.Request.RequesterPhone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20)
            .Matches(@"^[0-9+\-\s]+$").WithMessage("Phone number contains invalid characters.")
            .MinimumLength(10).WithMessage("Enter a valid phone number (at least 10 digits).");
        RuleFor(x => x.Request.ClinicContactNumber)
            .MaximumLength(20)
            .Matches(@"^[0-9+\-\s]*$").WithMessage("Contact number contains invalid characters.")
            .MinimumLength(10).WithMessage("Enter a valid contact number (at least 10 digits).")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.ClinicContactNumber));
        RuleFor(x => x.Request.OfficialEmail)
            .EmailAddress().WithMessage("Official email is not a valid email address.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.OfficialEmail));
        RuleFor(x => x.Request.Website)
            .MaximumLength(256)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == "http" || uri.Scheme == "https"))
            .WithMessage("Website must be a valid URL (e.g. https://example.com).")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Website));

        RuleFor(x => x.Request.DoctorName)
            .NotEmpty().WithMessage("Doctor name is required.")
            .MaximumLength(150).WithMessage("Doctor name must not exceed 150 characters.");
        RuleFor(x => x.Request.MedicalRegistrationNumber)
            .NotEmpty().WithMessage("Medical registration number is required.")
            .MaximumLength(50).WithMessage("Medical registration number must not exceed 50 characters.");
        RuleFor(x => x.Request.MedicalCouncil)
            .NotEmpty().WithMessage("Medical council is required.")
            .MaximumLength(150).WithMessage("Medical council must not exceed 150 characters.");
        RuleFor(x => x.Request.ExpectedStaffCount)
            .Must(v => v == (int)v).WithMessage("Staff count must be a whole number.")
            .InclusiveBetween(1, 500).WithMessage("Staff count must be between 1 and 500.");
        RuleFor(x => x.Request.ReferralSource).MaximumLength(100).WithMessage("Referral source must not exceed 100 characters.");
        RuleFor(x => x.Request.Notes).MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}
