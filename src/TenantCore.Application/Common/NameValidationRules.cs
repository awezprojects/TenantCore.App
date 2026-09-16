using System.Text.RegularExpressions;

namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on person-name fields.
public static class NameValidationRules
{
    private static readonly Regex ValidName = new(@"^[A-Za-z][A-Za-z '\-]*$", RegexOptions.Compiled);

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && ValidName.IsMatch(name.Trim());
}
