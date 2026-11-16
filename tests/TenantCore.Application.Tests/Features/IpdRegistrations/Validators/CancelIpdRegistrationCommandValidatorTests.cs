using FluentAssertions;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Validators;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Validators;

public class CancelIpdRegistrationCommandValidatorTests
{
    private readonly CancelIpdRegistrationCommandValidator _validator = new();

    private static CancelIpdRegistrationCommand ValidCommand(string? reason = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), reason);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
        => _validator.Validate(ValidCommand("Wrong patient admitted")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_FailsWithError()
        => _validator.Validate(ValidCommand() with { Id = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
        => _validator.Validate(ValidCommand() with { ApplicationId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NoReason_PassesValidation()
        => _validator.Validate(ValidCommand(null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ReasonAtMaxLength_PassesValidation()
        => _validator.Validate(ValidCommand(new string('a', 500))).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ReasonExceedsMaxLength_FailsWithError()
        => _validator.Validate(ValidCommand(new string('a', 501))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("Reason; <iframe src=x></iframe>")]
    public void Validate_ReasonContainsUnsafeContent_FailsWithError(string unsafeReason)
        => _validator.Validate(ValidCommand(unsafeReason)).IsValid.Should().BeFalse();
}
