using FluentAssertions;
using TenantCore.Application.Features.Prescriptions.Emails;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Prescriptions.Emails;

public class EmailTemplateThemeCatalogTests
{
    [Fact]
    public void All_DefinesEveryEnumValueExactlyOnce()
    {
        var enumValues = Enum.GetValues<EmailTemplateTheme>();

        EmailTemplateThemeCatalog.All.Should().HaveCount(enumValues.Length);
        EmailTemplateThemeCatalog.All.Select(t => t.Theme).Should().BeEquivalentTo(enumValues);
    }

    [Fact]
    public void All_ThemesHaveDistinctHeaderColors()
    {
        EmailTemplateThemeCatalog.All.Select(t => t.HeaderBackground)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ThemesHaveDistinctDisplayNames()
    {
        EmailTemplateThemeCatalog.All.Select(t => t.DisplayName)
            .Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(EmailTemplateTheme.AzureClassic)]
    [InlineData(EmailTemplateTheme.EmeraldCare)]
    [InlineData(EmailTemplateTheme.SunsetRose)]
    [InlineData(EmailTemplateTheme.MidnightIndigo)]
    [InlineData(EmailTemplateTheme.MinimalSlate)]
    public void Get_ReturnsTokensWithFullPalette(EmailTemplateTheme theme)
    {
        var tokens = EmailTemplateThemeCatalog.Get(theme);

        tokens.Theme.Should().Be(theme);
        tokens.DisplayName.Should().NotBeNullOrWhiteSpace();
        tokens.Description.Should().NotBeNullOrWhiteSpace();
        tokens.HeaderBackground.Should().StartWith("#");
        tokens.HeaderText.Should().StartWith("#");
        tokens.Accent.Should().StartWith("#");
        tokens.BodyBackground.Should().StartWith("#");
        tokens.BodyText.Should().StartWith("#");
        tokens.TableHeaderBackground.Should().StartWith("#");
        tokens.TableHeaderText.Should().StartWith("#");
        tokens.ButtonBackground.Should().StartWith("#");
        tokens.FontFamily.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Get_UnknownThemeValue_FallsBackToAzureClassic()
    {
        var tokens = EmailTemplateThemeCatalog.Get((EmailTemplateTheme)999);

        tokens.Theme.Should().Be(EmailTemplateTheme.AzureClassic);
    }
}
