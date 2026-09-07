using FluentAssertions;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Validators;

namespace TenantCore.Application.Tests.Features.Rooms.Validators;

public class UpdateRoomCommandValidatorTests
{
    private readonly UpdateRoomCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new UpdateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "General", 500);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenPricePerDayIsNegative_ReturnsError()
    {
        var command = new UpdateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "General", -1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PricePerDay");
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = new UpdateRoomCommand(Guid.Empty, Guid.NewGuid(), "101", "General", 500);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }
}
