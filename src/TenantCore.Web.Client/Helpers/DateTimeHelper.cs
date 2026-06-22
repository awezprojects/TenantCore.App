namespace TenantCore.Web.Client.Helpers;

/// <summary>
/// Converts UTC datetimes (as stored in the database) to the user's browser-local timezone for display.
/// All timestamps stored via DateTime.UtcNow must go through these helpers before being shown to users.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>Returns the browser-local date and time string for a UTC-stored datetime.</summary>
    public static string ToLocalString(DateTime utc, string format = "dd MMM yyyy HH:mm") =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString(format);

    /// <summary>Returns the browser-local date-only string for a UTC-stored datetime.</summary>
    public static string ToLocalDate(DateTime utc, string format = "dd MMM yyyy") =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString(format);

    /// <summary>
    /// Returns the browser's UTC offset in whole minutes (e.g. +330 for IST UTC+5:30).
    /// Send this to the server alongside any local-date filter so the server can compute the
    /// correct UTC range from the user's local midnight boundaries.
    /// </summary>
    public static int BrowserUtcOffsetMinutes =>
        (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
}
