using FluentAssertions;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Validators;

public class CreateAmountHandoverCommandValidatorTests
{
    private readonly CreateAmountHandoverCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsValidResult()
    {
        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = Guid.NewGuid(),
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000
            },
            Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenCounterSessionIdEmpty_ReturnsError()
    {
        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = Guid.Empty,
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000
            },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("CounterSessionId"));
    }

    [Fact]
    public void Validate_WhenHandedOverByUserIdEmpty_ReturnsError()
    {
        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = Guid.NewGuid(),
                HandedOverToUserId = Guid.NewGuid(),
                Amount = 1000
            },
            Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("HandedOverByUserId"));
    }

    [Fact]
    public void Validate_WhenAmountNegative_ReturnsError()
    {
        var command = new CreateAmountHandoverCommand(
            new CreateAmountHandoverRequest
            {
                CounterSessionId = Guid.NewGuid(),
                HandedOverToUserId = Guid.NewGuid(),
                Amount = -100
            },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Amount"));
    }
}
