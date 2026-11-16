using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Validators;

public class CreateIpdRegistrationCommandValidatorTests
{
    private readonly CreateIpdRegistrationCommandValidator _validator = new();

    private static CreateIpdRegistrationCommand ValidCommand(Guid? bedId = null, string? notes = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", bedId, 2000m, notes);

    [Fact]
    public void Validate_ValidCommandWithoutBed_PassesValidation()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithBed_PassesValidation()
        => _validator.Validate(ValidCommand(Guid.NewGuid())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyBedIdGuid_FailsWithError()
        => _validator.Validate(ValidCommand(Guid.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
        => _validator.Validate(ValidCommand() with { ApplicationId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyPatientId_FailsWithError()
        => _validator.Validate(ValidCommand() with { PatientId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyDoctorName_FailsWithError(string? name)
        => _validator.Validate(ValidCommand() with { DoctorName = name! }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeInitialFee_FailsWithError()
        => _validator.Validate(ValidCommand() with { InitialFee = -1 }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroInitialFee_PassesValidation()
        => _validator.Validate(ValidCommand() with { InitialFee = 0 }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    public void Validate_AdmissionNotesContainsUnsafeContent_FailsWithError(string unsafeNotes)
        => _validator.Validate(ValidCommand(notes: unsafeNotes)).IsValid.Should().BeFalse();
}
