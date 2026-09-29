using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Validators;

namespace TenantCore.Application.Tests.Features.Onboarding.Validators;

public class RetryWorkflowTaskCommandValidatorTests
{
    private readonly RetryWorkflowTaskCommandValidator _validator = new();

    private static RetryWorkflowTaskCommand CreateCommand(Guid? taskId = null, Guid? adminUserId = null, string adminEmail = "admin@example.test") =>
        new(taskId ?? Guid.NewGuid(), adminUserId ?? Guid.NewGuid(), adminEmail);

    [Fact]
    public void Validate_ValidCommand_ReturnsValid() => _validator.Validate(CreateCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyTaskId_ReturnsError() =>
        _validator.Validate(CreateCommand(taskId: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "TaskId");

    [Fact]
    public void Validate_EmptyAdminUserId_ReturnsError() =>
        _validator.Validate(CreateCommand(adminUserId: Guid.Empty)).Errors.Should().Contain(e => e.PropertyName == "AdminUserId");

    [Fact]
    public void Validate_EmptyAdminEmail_ReturnsError() =>
        _validator.Validate(CreateCommand(adminEmail: string.Empty)).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");

    [Fact]
    public void Validate_InvalidAdminEmail_ReturnsError() =>
        _validator.Validate(CreateCommand(adminEmail: "not-an-email")).Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
}
