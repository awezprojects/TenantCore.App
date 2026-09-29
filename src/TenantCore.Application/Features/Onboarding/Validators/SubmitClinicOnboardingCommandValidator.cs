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

        RuleFor(x => x.Request.ClinicName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.PreferredClinicCode).NotEmpty().Length(3, 20)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Clinic code must contain only letters, digits and hyphens.");
        RuleFor(x => x.Request.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Pincode).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Pincode must be exactly 6 digits.");
        RuleFor(x => x.Request.RequesterPhone).NotEmpty().MaximumLength(20)
            .Matches(@"^[0-9+\-\s]+$").WithMessage("Phone number contains invalid characters.");
        RuleFor(x => x.Request.ClinicContactNumber).MaximumLength(20)
            .Matches(@"^[0-9+\-\s]*$").WithMessage("Contact number contains invalid characters.");
        RuleFor(x => x.Request.OfficialEmail).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.OfficialEmail));
        RuleFor(x => x.Request.Website).MaximumLength(256);

        RuleFor(x => x.Request.DoctorName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.MedicalRegistrationNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.MedicalCouncil).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.ExpectedStaffCount).InclusiveBetween(1, 500);
        RuleFor(x => x.Request.ReferralSource).MaximumLength(100);
        RuleFor(x => x.Request.Notes).MaximumLength(1000);
    }
}
