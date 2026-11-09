using FluentAssertions;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.OpdPayments.Validators;

public class ApplyOpdDiscountCommandValidatorTests
{
    private readonly ApplyOpdDiscountCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsValidResult()
    {
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = Guid.NewGuid(), Discount = 100 },
            Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenOpdRegistrationIdEmpty_ReturnsError()
    {
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = Guid.Empty, Discount = 100 },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("OpdRegistrationId"));
    }

    [Fact]
    public void Validate_WhenDiscountNegative_ReturnsError()
    {
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = Guid.NewGuid(), Discount = -1 },
            Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Discount"));
    }

    [Fact]
    public void Validate_WhenDiscountedByUserIdEmpty_ReturnsError()
    {
        // Who gave the discount must always be known — this is the field admin relies on to
        // tell a doctor-given discount apart from a reception-given one.
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = Guid.NewGuid(), Discount = 100 },
            Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("DiscountedByUserId"));
    }

    [Fact]
    public void Validate_WhenApplicationIdEmpty_ReturnsError()
    {
        var command = new ApplyOpdDiscountCommand(
            new ApplyOpdDiscountRequest { OpdRegistrationId = Guid.NewGuid(), Discount = 100 },
            Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("ApplicationId"));
    }
}
