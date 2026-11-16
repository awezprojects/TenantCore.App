using System.Text.RegularExpressions;

namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on short, structured
// free-text fields (ward/room/bed names, cancellation/discount reasons, etc.).
//
// Deliberately an allow-list, not a block-list: a "reject these specific characters" check
// can always miss a new encoding of a script/HTML payload, but an allow-list of exactly the
// characters legitimate business text needs cannot let anything else through, by construction.
// Prefer this over HtmlSanitizationRules for any field that doesn't need multi-line text.
public static class SafeTextValidationRules
{
    private static readonly Regex SafeText = new(@"^[A-Za-z0-9\s.,'\-\/()]*$", RegexOptions.Compiled);

    public static bool IsSafeText(string? value) => value is null || SafeText.IsMatch(value);
}
