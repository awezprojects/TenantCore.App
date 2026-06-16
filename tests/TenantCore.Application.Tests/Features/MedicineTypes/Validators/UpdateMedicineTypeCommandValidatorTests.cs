using FluentAssertions;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Validators;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Validators;

public class UpdateMedicineTypeCommandValidatorTests
{
    private readonly UpdateMedicineTypeCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new UpdateMedicineTypeCommand(Guid.NewGuid(), "Tablet", "Oral solid dosage form", true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = new UpdateMedicineTypeCommand(Guid.Empty, "Tablet", "Oral solid dosage form", true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ReturnsError()
    {
        var command = new UpdateMedicineTypeCommand(Guid.NewGuid(), string.Empty, "Oral solid dosage form", true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ReturnsError()
    {
        var command = new UpdateMedicineTypeCommand(Guid.NewGuid(), "Tablet", new string('x', 501), true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Description");
    }
}
