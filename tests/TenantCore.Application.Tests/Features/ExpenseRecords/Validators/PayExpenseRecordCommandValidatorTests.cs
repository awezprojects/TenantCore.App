using FluentAssertions;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.ExpenseRecords.Validators;

public class PayExpenseRecordCommandValidatorTests
{
    private readonly PayExpenseRecordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new PayExpenseRecordCommand(Guid.NewGuid(), new PayExpenseRecordRequest { PayAmount = 100 }, Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPayAmountIsZero_ReturnsError()
    {
        var command = new PayExpenseRecordCommand(Guid.NewGuid(), new PayExpenseRecordRequest { PayAmount = 0 }, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("PayAmount"));
    }

    [Fact]
    public void Validate_WhenPayAmountIsNegative_ReturnsError()
    {
        var command = new PayExpenseRecordCommand(Guid.NewGuid(), new PayExpenseRecordRequest { PayAmount = -50 }, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("PayAmount"));
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = new PayExpenseRecordCommand(Guid.Empty, new PayExpenseRecordRequest { PayAmount = 100 }, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }
}
