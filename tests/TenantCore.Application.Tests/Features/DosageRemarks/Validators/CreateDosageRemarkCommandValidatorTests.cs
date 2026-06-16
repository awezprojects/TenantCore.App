using FluentAssertions;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Validators;

public class CreateDosageRemarkCommandValidatorTests
{
    private readonly CreateDosageRemarkCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new CreateDosageRemarkCommand(
            Guid.NewGuid(),
            MedicineFormType.Tab,
            "Take after food",
            "भोजन के बाद लें",
            "जेवणानंतर घ्या");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = new CreateDosageRemarkCommand(
            Guid.Empty,
            MedicineFormType.Tab,
            "Take after food",
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateDosageRemarkCommand.ApplicationId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenRemarkEnglishIsEmpty_ReturnsValidationError(string remarkEnglish)
    {
        var command = new CreateDosageRemarkCommand(
            Guid.NewGuid(),
            MedicineFormType.Tab,
            remarkEnglish,
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateDosageRemarkCommand.RemarkEnglish));
    }

    [Fact]
    public void Validate_WhenRemarkEnglishExceedsMaxLength_ReturnsValidationError()
    {
        var command = new CreateDosageRemarkCommand(
            Guid.NewGuid(),
            MedicineFormType.Tab,
            new string('x', 501),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateDosageRemarkCommand.RemarkEnglish));
    }

    [Fact]
    public void Validate_WhenMedicineFormIsInvalid_ReturnsValidationError()
    {
        var command = new CreateDosageRemarkCommand(
            Guid.NewGuid(),
            (MedicineFormType)999,
            "Take after food",
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateDosageRemarkCommand.MedicineForm));
    }
}
