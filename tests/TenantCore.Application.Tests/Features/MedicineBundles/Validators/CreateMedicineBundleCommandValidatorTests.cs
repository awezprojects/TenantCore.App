using FluentAssertions;
using TenantCore.Application.Features.MedicineBundles.Commands;
using TenantCore.Application.Features.MedicineBundles.Validators;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.MedicineBundles.Validators;

public class CreateMedicineBundleCommandValidatorTests
{
    private readonly CreateMedicineBundleCommandValidator _validator = new();

    private static CreateMedicineBundleItemDto ValidItem() => new(
        Guid.NewGuid(), "Folvite 5mg", null, MedicineFormType.Tab, "5mg", "tablet",
        1, null, null, null, 30, "OD", "Morning", "After meals", 0);

    private static CreateMedicineBundleCommand ValidCommand() => new(
        Guid.NewGuid(),
        new CreateMedicineBundleDto(Guid.NewGuid(), "Dr. Smith", "1st Trimester Care", 30, "Standard antenatal set", [ValidItem()]));

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ReturnsValidationError()
    {
        var command = ValidCommand();
        command = command with { Request = command.Request with { Name = "" } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains(nameof(CreateMedicineBundleDto.Name)));
    }

    [Fact]
    public void Validate_WhenDurationDaysIsZero_ReturnsValidationError()
    {
        var command = ValidCommand();
        command = command with { Request = command.Request with { DurationDays = 0 } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains(nameof(CreateMedicineBundleDto.DurationDays)));
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
    public void Validate_WhenItemMedicineIdIsEmpty_ReturnsValidationError()
    {
        var badItem = ValidItem() with { MedicineId = Guid.Empty };
        var command = ValidCommand();
        command = command with { Request = command.Request with { Items = [badItem] } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains(nameof(CreateMedicineBundleItemDto.MedicineId)));
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = ValidCommand() with { ApplicationId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateMedicineBundleCommand.ApplicationId));
    }
}
