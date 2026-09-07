namespace TenantCore.Application.Common;

// Reusable boundary predicates for FluentValidation .Must(...) rules on date/time fields.
public static class DateValidationRules
{
    public const int DefaultFutureLimitDays = 90;
    public const int DefaultPastLimitYears = 1;
    public const int DateOfBirthMaxAgeYears = 120;
    public const int LmpMaxAgeMonths = 10;

    public static bool IsNotInFuture(DateOnly date) => date <= DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsNotInFuture(DateTime date) => date <= DateTime.UtcNow;

    public static bool IsWithinFutureLimit(DateTime date, int maxDaysAhead = DefaultFutureLimitDays) =>
        date <= DateTime.UtcNow.AddDays(maxDaysAhead);

    public static bool IsWithinPastLimit(DateTime date, int maxYearsAgo = DefaultPastLimitYears) =>
        date >= DateTime.UtcNow.AddYears(-maxYearsAgo);

    public static bool IsValidOperationalDate(DateTime date, int maxYearsAgo = DefaultPastLimitYears) =>
        IsNotInFuture(date) && IsWithinPastLimit(date, maxYearsAgo);

    public static bool IsValidDateOfBirth(DateOnly dateOfBirth) =>
        IsNotInFuture(dateOfBirth) && dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-DateOfBirthMaxAgeYears);

    public static bool IsValidLmp(DateOnly lmp) =>
        IsNotInFuture(lmp) && lmp >= DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-LmpMaxAgeMonths);

    public static bool IsValidNextVisitDate(DateTime date) =>
        date >= DateTime.UtcNow.Date && IsWithinFutureLimit(date);
}
