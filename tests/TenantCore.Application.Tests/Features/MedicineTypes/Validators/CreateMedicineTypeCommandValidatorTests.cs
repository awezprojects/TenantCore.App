using FluentAssertions;
using TenantCore.Application.Features.MedicineTypes.Validators;
using TenantCore.Application.Tests.TestData;

namespace TenantCore.Application.Tests.Features.MedicineTypes.Validators;

public class CreateMedicineTypeCommandValidatorTests
{
    private readonly CreateMedicineTypeCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenNameIsEmpty_ReturnsValidationError(string name)
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand(name);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ReturnsValidationError()
    {
        var command = ApplicationTestData.CreateMedicineTypeCommand(description: ApplicationTestData.DescriptionOverMaxLength);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Description");
    }
}
