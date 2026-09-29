using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

public class RejectClinicOnboardingCommandValidatorTests
{
    private readonly RejectClinicOnboardingCommandValidator _validator = new();

    private static RejectClinicOnboardingCommand CreateCommand(Guid? id = null, string reason = "Not eligible", Guid? adminUserId = null, string adminEmail = "admin@example.test") =>
        new(id ?? Guid.NewGuid(), reason, adminUserId ?? Guid.NewGuid(), adminEmail);

    [Fact]
    public void Validate_ValidCommand_ReturnsValid() => _validator.Validate(CreateCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_ReturnsError() =>
        _validator.Validate(CreateCommand(id: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "Id");

    [Fact]
    public void Validate_EmptyReason_ReturnsError() =>
        _validator.Validate(CreateCommand(reason: string.Empty)).Errors.Should().Contain(e => e.PropertyName == "Reason");

    [Fact]
    public void Validate_ReasonAtMaxLength_ReturnsValid() =>
        _validator.Validate(CreateCommand(reason: new string('a', 1000))).Errors.Should().NotContain(e => e.PropertyName == "Reason");

    [Fact]
    public void Validate_ReasonOverMaxLength_ReturnsError() =>
        _validator.Validate(CreateCommand(reason: new string('a', 1001))).Errors.Should().Contain(e => e.PropertyName == "Reason");

    [Fact]
    public void Validate_EmptyAdminUserId_ReturnsError() =>
        _validator.Validate(CreateCommand(adminUserId: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "AdminUserId");

    [Fact]
    public void Validate_InvalidAdminEmail_ReturnsError() =>
        _validator.Validate(CreateCommand(adminEmail: "not-an-email")).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");

    [Fact]
    public void Validate_EmptyAdminEmail_ReturnsError() =>
        _validator.Validate(CreateCommand(adminEmail: string.Empty)).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
}
