using FluentAssertions;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Validators;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Validators;

public class CreateDoctorFeeConfigCommandValidatorTests
{
    private readonly CreateDoctorFeeConfigCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsValidResult()
    {
        var command = new CreateDoctorFeeConfigCommand(
            new CreateDoctorFeeConfigRequest { DoctorProfileId = Guid.NewGuid(), VisitFee = 500 },
            Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDoctorProfileIdEmpty_ReturnsError()
    {
        var command = new CreateDoctorFeeConfigCommand(
            new CreateDoctorFeeConfigRequest { DoctorProfileId = Guid.Empty, VisitFee = 500 },
            Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("DoctorProfileId"));
    }

    [Fact]
    public void Validate_WhenVisitFeeNegative_ReturnsError()
    {
        var command = new CreateDoctorFeeConfigCommand(
            new CreateDoctorFeeConfigRequest { DoctorProfileId = Guid.NewGuid(), VisitFee = -1 },
            Guid.NewGuid());

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("VisitFee"));
    }

    [Fact]
    public void Validate_WhenApplicationIdEmpty_ReturnsError()
    {
        var command = new CreateDoctorFeeConfigCommand(
            new CreateDoctorFeeConfigRequest { DoctorProfileId = Guid.NewGuid(), VisitFee = 500 },
            Guid.Empty);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("ApplicationId"));
    }
}
