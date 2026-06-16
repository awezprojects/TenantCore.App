using FluentAssertions;
using TenantCore.Application.Features.MedicineDosageForms.Commands;
using TenantCore.Application.Features.MedicineDosageForms.Validators;

namespace TenantCore.Application.Tests.Features.MedicineDosageForms.Validators;

public class CreateMedicineDosageFormCommandValidatorTests
{
    private readonly CreateMedicineDosageFormCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", "Oral solid dosage form");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ReturnsValidResult()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameIsEmpty_ReturnsValidationError(string name)
    {
        var command = new CreateMedicineDosageFormCommand(name, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateMedicineDosageFormCommand.Name));
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ReturnsValidationError()
    {
        var command = new CreateMedicineDosageFormCommand(new string('x', 101), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateMedicineDosageFormCommand.Name));
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ReturnsValidationError()
    {
        var command = new CreateMedicineDosageFormCommand("Tablet", new string('x', 501));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateMedicineDosageFormCommand.Description));
    }
}
