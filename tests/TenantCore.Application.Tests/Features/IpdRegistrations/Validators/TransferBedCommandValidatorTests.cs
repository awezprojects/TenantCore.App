using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Validators;

public class TransferBedCommandValidatorTests
{
    private readonly TransferBedCommandValidator _validator = new();

    private static TransferBedCommand ValidCommand(string? notes = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), notes);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand("Moved to ICU for monitoring")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_FailsWithError()
        => _validator.Validate(ValidCommand() with { Id = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
        => _validator.Validate(ValidCommand() with { ApplicationId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyNewBedId_FailsWithError()
        => _validator.Validate(ValidCommand() with { NewBedId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyTransferredByUserId_FailsWithError()
        => _validator.Validate(ValidCommand() with { TransferredByUserId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NotesAtMaxLength_PassesValidation()
        => _validator.Validate(ValidCommand(new string('a', 500))).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_NotesExceedsMaxLength_FailsWithError()
        => _validator.Validate(ValidCommand(new string('a', 501))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("Visit http://phishing.example for details")]
    public void Validate_NotesContainsUnsafeContent_FailsWithError(string unsafeNotes)
        => _validator.Validate(ValidCommand(unsafeNotes)).IsValid.Should().BeFalse();
}
