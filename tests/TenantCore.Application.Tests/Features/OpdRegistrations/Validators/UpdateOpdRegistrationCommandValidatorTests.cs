using FluentAssertions;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdRegistrations.Validators;

public class UpdateOpdRegistrationCommandValidatorTests
{
    private readonly UpdateOpdRegistrationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = new UpdateOpdRegistrationCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", 100, OpdStatus.Pending, null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenFeeIsZero_ReturnsValid()
    {
        var command = new UpdateOpdRegistrationCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", 0, OpdStatus.Pending, null);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenFeeIsNegative_ReturnsError()
    {
        var command = new UpdateOpdRegistrationCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", -1, OpdStatus.Pending, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Fee");
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = new UpdateOpdRegistrationCommand(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", 100, OpdStatus.Pending, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }
}
