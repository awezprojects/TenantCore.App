using FluentAssertions;
using TenantCore.Application.Features.UsgTemplates.Commands;
using TenantCore.Application.Features.UsgTemplates.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.UsgTemplates.Validators;

public class UpsertClinicUsgTemplateCommandValidatorTests
{
    private readonly UpsertClinicUsgTemplateCommandValidator _validator = new();

    private static UsgTemplateRowDto ValidRow() => new()
    {
        RowOrder     = 1,
        WeekLabel    = "6 Weeks",
        LmpDayOffset = 42,
        Activity     = "Dating Scan",
        Indication   = "Confirm viability and gestational age",
    };

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [ValidRow()] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_ReturnsValidationError()
    {
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [ValidRow()] },
            Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpsertClinicUsgTemplateCommand.ApplicationId));
    }

    [Fact]
    public void Validate_WhenRowsListIsEmpty_ReturnsValidationError()
    {
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("At least one row is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenRowWeekLabelIsEmpty_ReturnsValidationError(string weekLabel)
    {
        var row = ValidRow() with { WeekLabel = weekLabel };
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [row] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("WeekLabel"));
    }

    [Fact]
    public void Validate_WhenRowWeekLabelExceedsMaxLength_ReturnsValidationError()
    {
        var row = ValidRow() with { WeekLabel = new string('x', 51) };
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [row] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("WeekLabel"));
    }

    [Fact]
    public void Validate_WhenRowLmpDayOffsetIsZero_ReturnsValidationError()
    {
        var row = ValidRow() with { LmpDayOffset = 0 };
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [row] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("LmpDayOffset"));
    }

    [Fact]
    public void Validate_WhenRowActivityExceedsMaxLength_ReturnsValidationError()
    {
        var row = ValidRow() with { Activity = new string('x', 501) };
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [row] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Activity"));
    }

    [Fact]
    public void Validate_WhenRowIndicationExceedsMaxLength_ReturnsValidationError()
    {
        var row = ValidRow() with { Indication = new string('x', 501) };
        var command = new UpsertClinicUsgTemplateCommand(
            new UpsertClinicUsgTemplateRequest { Rows = [row] },
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Indication"));
    }
}
