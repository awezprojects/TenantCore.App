namespace TenantCore.Shared.Enums;

/// <summary>
/// Selectable visual theme for the patient prescription email.
/// One theme is chosen per clinic and stored on <c>PrescriptionConfig.EmailTheme</c>.
/// </summary>
public enum EmailTemplateTheme
{
    /// <summary>Professional deep-blue banner with a bordered medicine table.</summary>
    AzureClassic = 1,

    /// <summary>Teal/green card layout with rounded pills and soft gradient header.</summary>
    EmeraldCare = 2,

    /// <summary>Warm rose/coral gradient banner with a friendly, rounded appointment card.</summary>
    SunsetRose = 3,

    /// <summary>Dark indigo high-contrast header with white alternate rows.</summary>
    MidnightIndigo = 4,

    /// <summary>Flat monochrome slate design with thin dividers, no gradients.</summary>
    MinimalSlate = 5
}
