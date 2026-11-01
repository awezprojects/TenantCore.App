using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

public class RetryClinicOnboardingCommandValidatorTests
{
    private readonly RetryClinicOnboardingCommandValidator _validator = new();

    private static RetryClinicOnboardingCommand CreateCommand(Guid? id = null, string? clinicCode = null, Guid? adminUserId = null, string adminEmail = "admin@example.test") =>
        new(id ?? Guid.NewGuid(), clinicCode, adminUserId ?? Guid.NewGuid(), adminEmail);

    [Fact]
    public void Validate_ValidCommandWithoutCodeOverride_ReturnsValid() => _validator.Validate(CreateCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_ReturnsError() =>
        _validator.Validate(CreateCommand(id: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "Id");

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Validate_ClinicCodeOutsideLengthRange_ReturnsError(string code) =>
        _validator.Validate(CreateCommand(clinicCode: code)).Errors.Should().Contain(e => e.PropertyName == "ClinicCode");

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLMNOPQRST")]
    public void Validate_ClinicCodeAtBoundary_ReturnsValid(string code) =>
        _validator.Validate(CreateCommand(clinicCode: code)).Errors.Should().NotContain(e => e.PropertyName == "ClinicCode");

    [Fact]
    public void Validate_ClinicCodeWithInvalidCharacters_ReturnsError() =>
        _validator.Validate(CreateCommand(clinicCode: "BAD CODE!")).Errors.Should().Contain(e => e.PropertyName == "ClinicCode");

    [Fact]
    public void Validate_ClinicCodeNull_ReturnsValid() =>
        _validator.Validate(CreateCommand(clinicCode: null)).Errors.Should().NotContain(e => e.PropertyName == "ClinicCode");

    [Fact]
    public void Validate_EmptyAdminUserId_ReturnsError() =>
        _validator.Validate(CreateCommand(adminUserId: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "AdminUserId");

    [Fact]
    public void Validate_InvalidAdminEmail_ReturnsError() =>
        _validator.Validate(CreateCommand(adminEmail: "not-an-email")).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
}
