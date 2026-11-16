using System.Text.RegularExpressions;

namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on string-typed fields
// that must hold digits only (e.g. Aadhaar, PIN code) — a strongly-typed numeric property
// (decimal/int) already gets this for free from model binding and does not need this helper.
public static class NumericValidationRules
{
    private static readonly Regex DigitsOnly = new(@"^[0-9]+$", RegexOptions.Compiled);

    public static bool IsDigitsOnly(string? value) =>
        !string.IsNullOrWhiteSpace(value) && DigitsOnly.IsMatch(value.Trim());
}
