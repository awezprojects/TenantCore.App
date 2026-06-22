using FluentAssertions;
using TenantCore.Application.Features.ExpenseRecords.Commands;
using TenantCore.Application.Features.ExpenseRecords.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.ExpenseRecords.Validators;

public class CreateExpenseRecordCommandValidatorTests
{
    private readonly CreateExpenseRecordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsValidResult()
    {
        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.NewGuid(), Amount = 200 },
            Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenExpenseCategoryIdEmpty_ReturnsError()
    {
        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.Empty, Amount = 200 },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("ExpenseCategoryId"));
    }

    [Fact]
    public void Validate_WhenAmountZero_ReturnsError()
    {
        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.NewGuid(), Amount = 0 },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Amount"));
    }

    [Fact]
    public void Validate_WhenRecordedByUserIdEmpty_ReturnsError()
    {
        var command = new CreateExpenseRecordCommand(
            new CreateExpenseRecordRequest { ExpenseCategoryId = Guid.NewGuid(), Amount = 100 },
            Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("RecordedByUserId"));
    }
}
