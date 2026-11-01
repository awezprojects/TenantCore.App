using FluentValidation;

namespace TenantCore.Application.Common;

/// <summary>
/// The admin identity every PlatformAdmin command carries. The Admin portal takes it from the
/// signed-in principal — it is never typed by a person — but it is still validated here so a
/// malformed internal call fails as a 400 rather than writing a blank audit trail.
/// </summary>
public static class AdminActorValidationExtensions
{
    public static IRuleBuilderOptions<T, Guid> AdminUserIdRules<T>(this IRuleBuilder<T, Guid> rule)
        => rule.NotEmpty().WithMessage("The acting admin's user id is required.");

    public static IRuleBuilderOptions<T, string> AdminEmailRules<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().WithMessage("The acting admin's email is required.")
               .EmailAddress().WithMessage("The acting admin's email must be a valid email address.")
               .MaximumLength(256);

    /// <summary>Free-text reason shared by suspend, grant and cancel-upcoming.</summary>
    public static IRuleBuilderOptions<T, string> ReasonRules<T>(this IRuleBuilder<T, string> rule, string what)
        => rule.NotEmpty().WithMessage($"A {what} is required.")
               .MinimumLength(3).WithMessage($"The {what} must be at least 3 characters.")
               .MaximumLength(500);
}
