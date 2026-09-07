using FluentAssertions;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Validators;

public class CollectAmountHandoverCommandValidatorTests
{
    private readonly CollectAmountHandoverCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new CollectAmountHandoverCommand(
            new CollectAmountHandoverRequest { CounterSessionId = Guid.NewGuid(), Amount = 500 },
            Guid.NewGuid(), "Reception", Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenAmountIsZero_ReturnsValid()
    {
        var command = new CollectAmountHandoverCommand(
            new CollectAmountHandoverRequest { CounterSessionId = Guid.NewGuid(), Amount = 0 },
            Guid.NewGuid(), "Reception", Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenAmountIsNegative_ReturnsError()
    {
        var command = new CollectAmountHandoverCommand(
            new CollectAmountHandoverRequest { CounterSessionId = Guid.NewGuid(), Amount = -1 },
            Guid.NewGuid(), "Reception", Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Amount"));
    }

    [Fact]
    public void Validate_WhenCollectedByUserIdIsEmpty_ReturnsError()
    {
        var command = new CollectAmountHandoverCommand(
            new CollectAmountHandoverRequest { CounterSessionId = Guid.NewGuid(), Amount = 100 },
            Guid.Empty, "Reception", Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "CollectedByUserId");
    }
}
