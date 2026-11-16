using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Validators;

public class DischargePatientCommandValidatorTests
{
    private readonly DischargePatientCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(new DischargePatientCommand(Guid.NewGuid(), Guid.NewGuid(), "Recovered well")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_FailsWithError()
        => _validator.Validate(new DischargePatientCommand(Guid.Empty, Guid.NewGuid(), null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NoDischargeNotes_PassesValidation()
        => _validator.Validate(new DischargePatientCommand(Guid.NewGuid(), Guid.NewGuid(), null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_DischargeNotesExceedsMaxLength_FailsWithError()
        => _validator.Validate(new DischargePatientCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 1001))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("Follow up at http://phishing.example")]
    public void Validate_DischargeNotesContainsUnsafeContent_FailsWithError(string unsafeNotes)
        => _validator.Validate(new DischargePatientCommand(Guid.NewGuid(), Guid.NewGuid(), unsafeNotes)).IsValid.Should().BeFalse();
}
