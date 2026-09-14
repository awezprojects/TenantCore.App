using FluentAssertions;
using TenantCore.Application.Features.PrescriptionConfig.Commands;
using TenantCore.Application.Features.PrescriptionConfig.Validators;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PrescriptionConfig.Validators;

public class UpsertPrescriptionConfigCommandValidatorTests
{
    private readonly UpsertPrescriptionConfigCommandValidator _validator = new();

    [Theory]
    [InlineData(EmailTemplateTheme.AzureClassic)]
    [InlineData(EmailTemplateTheme.EmeraldCare)]
    [InlineData(EmailTemplateTheme.SunsetRose)]
    [InlineData(EmailTemplateTheme.MidnightIndigo)]
    [InlineData(EmailTemplateTheme.MinimalSlate)]
    public void Validate_EveryDefinedEmailTheme_Passes(EmailTemplateTheme theme)
    {
        _validator.Validate(Build(theme)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UndefinedEmailTheme_Fails()
    {
        var result = _validator.Validate(Build((EmailTemplateTheme)42));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("EmailTheme"));
    }

    [Fact]
    public void Validate_EmptyApplicationId_Fails()
    {
        var command = new UpsertPrescriptionConfigCommand(
            Guid.Empty, PrescriptionLanguage.English, 0, 0, 0, 0, false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }

    [Fact]
    public void Validate_UndefinedDefaultLanguage_Fails()
    {
        var command = new UpsertPrescriptionConfigCommand(
            Guid.NewGuid(), (PrescriptionLanguage)99, 0, 0, 0, 0, false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DefaultLanguage"));
    }

    private static UpsertPrescriptionConfigCommand Build(EmailTemplateTheme theme) =>
        new(Guid.NewGuid(), PrescriptionLanguage.English, 10, 10, 10, 10, false, theme);
}
