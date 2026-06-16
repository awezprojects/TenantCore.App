using FluentAssertions;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Validators;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Validators;

public class UpdateMedicineDosageFormCommandValidatorTests
{
    private readonly UpdateMedicineDosageFormCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.NewGuid(), "Capsule", "Encapsulated form", true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ReturnsValidResult()
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.NewGuid(), "Capsule", null, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsValidationError()
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.Empty, "Capsule", null, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateMedicineDosageFormCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameIsEmpty_ReturnsValidationError(string name)
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.NewGuid(), name, null, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateMedicineDosageFormCommand.Name));
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ReturnsValidationError()
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.NewGuid(), new string('x', 101), null, true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateMedicineDosageFormCommand.Name));
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ReturnsValidationError()
    {
        var command = new UpdateMedicineDosageFormCommand(Guid.NewGuid(), "Capsule", new string('x', 501), true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateMedicineDosageFormCommand.Description));
    }
}
