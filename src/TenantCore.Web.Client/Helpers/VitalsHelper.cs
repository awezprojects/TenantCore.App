namespace TenantCore.Web.Client.Helpers;

/// <summary>
/// Parsing helpers for the free-text vital-sign fields stored on prescriptions (e.g. VitalBP is
/// kept as a single "120/80" string rather than separate systolic/diastolic columns).
/// </summary>
public static class VitalsHelper
{
    /// <summary>Splits a "120/80" style blood-pressure reading into systolic/diastolic values.</summary>
    public static bool TryParseBp(string? raw, out double systolic, out double diastolic)
    {
        systolic = 0;
        diastolic = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var parts = raw.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && double.TryParse(parts[0], out systolic)
            && double.TryParse(parts[1], out diastolic);
    }
}
