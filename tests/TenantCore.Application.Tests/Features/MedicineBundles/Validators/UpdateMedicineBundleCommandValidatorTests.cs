using FluentAssertions;
using TenantCore.Application.Features.MedicineBundles.Commands;
using TenantCore.Application.Features.MedicineBundles.Validators;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.MedicineBundles.Validators;

public class UpdateMedicineBundleCommandValidatorTests
{
    private readonly UpdateMedicineBundleCommandValidator _validator = new();

    private static CreateMedicineBundleItemDto ValidItem() => new(
        Guid.NewGuid(), "Folvite 5mg", null, MedicineFormType.Tab, "5mg", "tablet",
        1, null, null, null, 30, "OD", "Morning", "After meals", 0);

    private static UpdateMedicineBundleCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(),
        new UpdateMedicineBundleDto("1st Trimester Care", 30, "Standard antenatal set", [ValidItem()]));

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsValidationError()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateMedicineBundleCommand.Id));
    }

    [Fact]
    public void Validate_WhenItemsIsEmpty_ReturnsValidationError()
    {
        var command = ValidCommand();
        command = command with { Request = command.Request with { Items = [] } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("at least one medicine"));
    }

    [Fact]
    public void Validate_WhenDurationDaysIsNegative_ReturnsValidationError()
    {
        var command = ValidCommand();
        command = command with { Request = command.Request with { DurationDays = -1 } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains(nameof(UpdateMedicineBundleDto.DurationDays)));
    }
}
