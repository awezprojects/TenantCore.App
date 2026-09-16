namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on free-text fields
// that must not carry HTML/script markup (e.g. address, notes).
public static class HtmlSanitizationRules
{
    public static bool HasNoHtmlTags(string? value) =>
        string.IsNullOrEmpty(value) || (!value.Contains('<') && !value.Contains('>'));
}
