using FluentAssertions;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Validators;

namespace TenantCore.Application.Tests.Features.Subscriptions.Validators;

public class SubscribeToPlanCommandValidatorTests
{
    private readonly SubscribeToPlanCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new SubscribeToPlanCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
    {
        var command = new SubscribeToPlanCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }

    [Fact]
    public void Validate_EmptySubscriptionPlanId_FailsWithError()
    {
        var command = new SubscribeToPlanCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("SubscriptionPlanId"));
    }

    [Fact]
    public void Validate_EmptyActingUserId_FailsWithError()
    {
        var command = new SubscribeToPlanCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ActingUserId"));
    }
}
