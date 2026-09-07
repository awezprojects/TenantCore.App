using FluentAssertions;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Validators;

namespace TenantCore.Application.Tests.Features.Rooms.Validators;

public class CreateRoomCommandValidatorTests
{
    private readonly CreateRoomCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "General", 500);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPricePerDayIsZero_ReturnsValid()
    {
        var command = new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "General", 0);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPricePerDayIsNegative_ReturnsError()
    {
        var command = new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "General", -0.01m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PricePerDay");
    }

    [Fact]
    public void Validate_WhenRoomNumberIsEmpty_ReturnsError()
    {
        var command = new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, "General", 500);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "RoomNumber");
    }
}
