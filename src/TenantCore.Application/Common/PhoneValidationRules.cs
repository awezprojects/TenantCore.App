using System.Text.RegularExpressions;

namespace TenantCore.Application.Common;

// Reusable boundary predicate for FluentValidation .Must(...) rules on phone number fields.
public static class PhoneValidationRules
{
    private static readonly Regex TenDigitPhone = new(@"^[0-9]{10}$", RegexOptions.Compiled);

    public static bool IsValidPhoneNumber(string? phone) =>
        !string.IsNullOrWhiteSpace(phone) && TenDigitPhone.IsMatch(phone.Trim());
}
