using FluentAssertions;
using TenantCore.Application.Features.DosageRemarks.Commands;
using TenantCore.Application.Features.DosageRemarks.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DosageRemarks.Validators;

public class UpdateDosageRemarkCommandValidatorTests
{
    private readonly UpdateDosageRemarkCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new UpdateDosageRemarkCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MedicineFormType.Tab,
            "Take after food",
            "भोजन के बाद लें",
            "जेवणानंतर घ्या",
            true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsValidationError()
    {
        var command = new UpdateDosageRemarkCommand(
            Guid.Empty,
            Guid.NewGuid(),
            MedicineFormType.Tab,
            "Take after food",
            null,
            null,
            true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateDosageRemarkCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenRemarkEnglishIsEmpty_ReturnsValidationError(string remarkEnglish)
    {
        var command = new UpdateDosageRemarkCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MedicineFormType.Tab,
            remarkEnglish,
            null,
            null,
            true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateDosageRemarkCommand.RemarkEnglish));
    }

    [Fact]
    public void Validate_WhenRemarkEnglishExceedsMaxLength_ReturnsValidationError()
    {
        var command = new UpdateDosageRemarkCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MedicineFormType.Tab,
            new string('x', 501),
            null,
            null,
            true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateDosageRemarkCommand.RemarkEnglish));
    }
}
