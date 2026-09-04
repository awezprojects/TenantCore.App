using FluentAssertions;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Validators;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Tests.Features.Subscriptions.Validators;

public class UpdateSubscriptionAlertSettingCommandValidatorTests
{
    private readonly UpdateSubscriptionAlertSettingCommandValidator _validator = new();

    private static UpdateSubscriptionAlertSettingCommand ValidCommand() => new(
        Guid.NewGuid(),
        new UpdateSubscriptionAlertSettingRequest
        {
            Subject = "Subject",
            Headline = "Headline",
            BodyMessage = "Body",
            IsEnabled = true,
            DisplayOrder = 1
        });

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_FailsWithError()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Id"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptySubject_FailsWithError(string? subject)
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { Subject = subject! } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Subject"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyHeadline_FailsWithError(string? headline)
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { Headline = headline! } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Headline"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyBodyMessage_FailsWithError(string? bodyMessage)
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { BodyMessage = bodyMessage! } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("BodyMessage"));
    }

    [Fact]
    public void Validate_SubjectAtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { Subject = new string('a', 200) } };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SubjectExceedsMaxLength_FailsWithError()
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { Subject = new string('a', 201) } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Subject"));
    }

    [Fact]
    public void Validate_BodyMessageAtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { BodyMessage = new string('a', 1000) } };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BodyMessageExceedsMaxLength_FailsWithError()
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { BodyMessage = new string('a', 1001) } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("BodyMessage"));
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_FailsWithError()
    {
        var command = ValidCommand() with { Request = ValidCommand().Request with { DisplayOrder = -1 } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DisplayOrder"));
    }
}
