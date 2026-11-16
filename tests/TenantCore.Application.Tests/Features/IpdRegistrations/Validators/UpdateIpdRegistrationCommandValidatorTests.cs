using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Validators;

public class UpdateIpdRegistrationCommandValidatorTests
{
    private readonly UpdateIpdRegistrationCommandValidator _validator = new();

    private static UpdateIpdRegistrationCommand ValidCommand(string? notes = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", notes);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyDoctorName_FailsWithError(string? name)
        => _validator.Validate(ValidCommand() with { DoctorName = name! }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_DoctorNameAtMaxLength_PassesValidation()
        => _validator.Validate(ValidCommand() with { DoctorName = new string('a', 200) }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_DoctorNameExceedsMaxLength_FailsWithError()
        => _validator.Validate(ValidCommand() with { DoctorName = new string('a', 201) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_DoctorNameWithTitleAndPeriod_PassesValidation()
        => _validator.Validate(ValidCommand() with { DoctorName = "Dr. Smith" }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
        => _validator.Validate(ValidCommand() with { ApplicationId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("See www.phishing-example.com for the report")]
    public void Validate_AdmissionNotesContainsUnsafeContent_FailsWithError(string unsafeNotes)
        => _validator.Validate(ValidCommand(unsafeNotes)).IsValid.Should().BeFalse();
}
