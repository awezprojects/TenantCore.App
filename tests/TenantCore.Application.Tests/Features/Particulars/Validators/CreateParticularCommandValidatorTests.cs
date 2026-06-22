using FluentAssertions;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Application.Features.Particulars.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Particulars.Validators;

public class CreateParticularCommandValidatorTests
{
    private readonly CreateParticularCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsValidResult()
    {
        var command = new CreateParticularCommand(
            new CreateParticularRequest { Name = "X-Ray", DefaultAmount = 500 }, Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameEmpty_ReturnsError()
    {
        var command = new CreateParticularCommand(
            new CreateParticularRequest { Name = "", DefaultAmount = 500 }, Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Name"));
    }

    [Fact]
    public void Validate_WhenDefaultAmountNegative_ReturnsError()
    {
        var command = new CreateParticularCommand(
            new CreateParticularRequest { Name = "X-Ray", DefaultAmount = -10 }, Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("DefaultAmount"));
    }

    [Fact]
    public void Validate_WhenApplicationIdEmpty_ReturnsError()
    {
        var command = new CreateParticularCommand(
            new CreateParticularRequest { Name = "X-Ray", DefaultAmount = 500 }, Guid.Empty);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("ApplicationId"));
    }
}
