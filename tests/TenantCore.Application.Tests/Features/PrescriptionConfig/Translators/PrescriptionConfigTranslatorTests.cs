using FluentAssertions;
using TenantCore.Application.Features.PrescriptionConfig.Translators;
using TenantCore.Shared.Enums;
using Config = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Tests.Features.PrescriptionConfig.Translators;

public class PrescriptionConfigTranslatorTests
{
    [Fact]
    public void ToDto_MapsAllFieldsIncludingEmailTheme()
    {
        var config = Config.Create(
            Guid.NewGuid(), PrescriptionLanguage.Hindi, 5, 6, 7, 8, true, EmailTemplateTheme.SunsetRose);

        var dto = PrescriptionConfigTranslator.ToDto(config);

        dto.ApplicationId.Should().Be(config.ApplicationId);
        dto.DefaultLanguage.Should().Be(PrescriptionLanguage.Hindi);
        dto.PrintMarginTop.Should().Be(5);
        dto.PrintMarginRight.Should().Be(6);
        dto.PrintMarginBottom.Should().Be(7);
        dto.PrintMarginLeft.Should().Be(8);
        dto.HideClinicHeader.Should().BeTrue();
        dto.EmailTheme.Should().Be(EmailTemplateTheme.SunsetRose);
    }

    [Theory]
    [InlineData(EmailTemplateTheme.AzureClassic)]
    [InlineData(EmailTemplateTheme.EmeraldCare)]
    [InlineData(EmailTemplateTheme.SunsetRose)]
    [InlineData(EmailTemplateTheme.MidnightIndigo)]
    [InlineData(EmailTemplateTheme.MinimalSlate)]
    public void ToDto_MapsEveryEmailTheme(EmailTemplateTheme theme)
    {
        var config = Config.Create(Guid.NewGuid(), PrescriptionLanguage.English, emailTheme: theme);

        PrescriptionConfigTranslator.ToDto(config).EmailTheme.Should().Be(theme);
    }
}
