using FluentAssertions;
using TenantCore.Application.Features.IpdDeposits.Commands;
using TenantCore.Application.Features.IpdDeposits.Validators;

namespace TenantCore.Application.Tests.Features.IpdDeposits.Validators;

public class CollectIpdDepositCommandValidatorTests
{
    private readonly CollectIpdDepositCommandValidator _validator = new();

    private static CollectIpdDepositCommand ValidCommand(decimal amount = 1000m, string? notes = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), amount, Guid.NewGuid(), notes);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand(1000m, "Advance payment")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ZeroAmount_FailsWithError()
        => _validator.Validate(ValidCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeAmount_FailsWithError()
        => _validator.Validate(ValidCommand(-100)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyIpdRegistrationId_FailsWithError()
        => _validator.Validate(ValidCommand() with { IpdRegistrationId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCollectedByUserId_FailsWithError()
        => _validator.Validate(ValidCommand() with { CollectedByUserId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("Receipt at http://phishing.example")]
    public void Validate_NotesContainsUnsafeContent_FailsWithError(string unsafeNotes)
        => _validator.Validate(ValidCommand(1000m, unsafeNotes)).IsValid.Should().BeFalse();
}
