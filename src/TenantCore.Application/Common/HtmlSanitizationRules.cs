using System.Text.RegularExpressions;

namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on free-text fields
// that must not carry HTML/script markup (e.g. address, notes).
public static class HtmlSanitizationRules
{
    public static bool HasNoHtmlTags(string? value) =>
        string.IsNullOrEmpty(value) || (!value.Contains('<') && !value.Contains('>'));

    // Broader than HasNoHtmlTags: a bare "< >" check misses a payload that encodes the
    // angle brackets as HTML entities, or one that needs no angle brackets at all
    // (javascript:/data: URIs, inline event-handler attributes). Use this as the default
    // for any new free-text field going forward — see ADR-010 Section 3.
    private static readonly Regex DangerousContent = new(
        @"<\s*script|<\s*iframe|javascript\s*:|vbscript\s*:|data\s*:\s*text/html|on\w+\s*=|&lt;\s*script|&#",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool HasNoScriptInjection(string? value) =>
        string.IsNullOrEmpty(value) || !DangerousContent.IsMatch(value);

    // For fields where a link is never a legitimate value (names, reasons, short notes) —
    // bare URLs and anchor markup are the concrete, checkable part of "phishing content."
    // Do not apply this to fields that may legitimately hold a URL (e.g. a clinic website).
    private static readonly Regex LinkLike = new(
        @"https?\s*:|www\.|<\s*a\s|href\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool HasNoLinks(string? value) =>
        string.IsNullOrEmpty(value) || !LinkLike.IsMatch(value);
}
