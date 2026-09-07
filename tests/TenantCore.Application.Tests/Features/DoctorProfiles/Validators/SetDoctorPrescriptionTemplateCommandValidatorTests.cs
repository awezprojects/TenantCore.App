using FluentAssertions;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.DoctorProfiles.Validators;

public class SetDoctorPrescriptionTemplateCommandValidatorTests
{
    private readonly SetDoctorPrescriptionTemplateCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValidResult()
    {
        var command = new SetDoctorPrescriptionTemplateCommand(Guid.NewGuid(), PrescriptionTemplate.Compact);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ReturnsValidationError()
    {
        var command = new SetDoctorPrescriptionTemplateCommand(Guid.Empty, PrescriptionTemplate.Classic);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetDoctorPrescriptionTemplateCommand.UserId));
    }

    [Fact]
    public void Validate_WhenTemplateIsUndefined_ReturnsValidationError()
    {
        var command = new SetDoctorPrescriptionTemplateCommand(Guid.NewGuid(), (PrescriptionTemplate)999);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SetDoctorPrescriptionTemplateCommand.Template));
    }
}
