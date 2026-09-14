using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Prescriptions.Emails;

/// <summary>
/// Pure visual/layout tokens for one prescription email theme.
/// No behaviour — consumed by <see cref="PrescriptionEmailBuilder"/>.
/// </summary>
public sealed record EmailThemeTokens(
    EmailTemplateTheme Theme,
    string DisplayName,
    string Description,
    string HeaderBackground,
    string? HeaderBackgroundEnd,
    string HeaderText,
    string Accent,
    string BodyBackground,
    string BodyText,
    string MutedText,
    string CardBackground,
    string CardBorder,
    string TableHeaderBackground,
    string TableHeaderText,
    string AlternateRowBackground,
    string ButtonBackground,
    string ButtonText,
    int CornerRadius,
    string FontFamily);

/// <summary>
/// The five selectable prescription email themes. Add a new theme here and to the
/// <see cref="EmailTemplateTheme"/> enum — <see cref="Get"/> falls back to Azure Classic.
/// </summary>
public static class EmailTemplateThemeCatalog
{
    private const string SansFont = "'Segoe UI', -apple-system, Arial, sans-serif";

    public static IReadOnlyList<EmailThemeTokens> All { get; } =
    [
        new(
            EmailTemplateTheme.AzureClassic, "Azure Classic",
            "Professional deep-blue banner with a bordered medicine table.",
            "#1565C0", "#1E88E5", "#FFFFFF", "#1E88E5",
            "#F5F8FC", "#1E293B", "#64748B", "#FFFFFF", "#DBEAFE",
            "#1565C0", "#FFFFFF", "#EFF6FF", "#1565C0", "#FFFFFF", 6, SansFont),

        new(
            EmailTemplateTheme.EmeraldCare, "Emerald Care",
            "Teal and green cards with soft gradient header and rounded pills.",
            "#0F766E", "#14B8A6", "#FFFFFF", "#14B8A6",
            "#F0FDFA", "#134E4A", "#4B7A75", "#FFFFFF", "#99F6E4",
            "#0F766E", "#FFFFFF", "#F0FDFA", "#0F766E", "#FFFFFF", 12, SansFont),

        new(
            EmailTemplateTheme.SunsetRose, "Sunset Rose",
            "Warm rose and coral gradient banner with a friendly appointment card.",
            "#E11D48", "#FB7185", "#FFFFFF", "#FB7185",
            "#FFF1F2", "#4C0519", "#9F1239", "#FFFFFF", "#FECDD3",
            "#E11D48", "#FFFFFF", "#FFF1F2", "#E11D48", "#FFFFFF", 14, SansFont),

        new(
            EmailTemplateTheme.MidnightIndigo, "Midnight Indigo",
            "Dark indigo high-contrast header with white alternate table rows.",
            "#312E81", "#4338CA", "#FFFFFF", "#6366F1",
            "#EEF2FF", "#1E1B4B", "#4F46E5", "#FFFFFF", "#C7D2FE",
            "#312E81", "#FFFFFF", "#EEF2FF", "#4338CA", "#FFFFFF", 8, SansFont),

        new(
            EmailTemplateTheme.MinimalSlate, "Minimal Slate",
            "Flat monochrome slate design with thin dividers and no gradients.",
            "#334155", null, "#FFFFFF", "#64748B",
            "#F8FAFC", "#0F172A", "#64748B", "#FFFFFF", "#E2E8F0",
            "#F1F5F9", "#334155", "#FFFFFF", "#334155", "#FFFFFF", 2, SansFont)
    ];

    /// <summary>Resolves tokens for a theme; unknown values fall back to Azure Classic.</summary>
    public static EmailThemeTokens Get(EmailTemplateTheme theme) =>
        All.FirstOrDefault(t => t.Theme == theme) ?? All[0];
}
