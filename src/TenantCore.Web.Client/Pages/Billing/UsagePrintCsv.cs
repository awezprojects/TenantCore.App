using System.Globalization;
using System.Text;

namespace TenantCore.Web.Client.Pages.Billing;

/// <summary>
/// Client-side CSV for the usage screens. RFC-4180 quoting plus a spreadsheet formula guard: a value
/// starting with '=', '+', '-', '@', a tab or a CR is prefixed with an apostrophe so it can never be
/// evaluated as a formula when the file is opened in Excel, Numbers or Sheets.
///
/// Deliberately a client-side helper with no package reference and no server-side generator — the
/// export is a secondary interaction served from the page's cached range (workspace rule 7).
/// </summary>
internal static class UsagePrintCsv
{
    /// <summary>Escapes one CSV cell (RFC-4180 quoting + formula-injection guard).</summary>
    internal static string Field(string value)
    {
        if (value.Length > 0 && (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
        {
            value = "'" + value;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? '"' + value.Replace("\"", "\"\"") + '"'
            : value;
    }

    /// <summary>Formats a value for CSV with the invariant culture, whatever the browser locale is.</summary>
    internal static string Field(decimal value) => Field(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Builds one CSV line from already-escaped cells.</summary>
    internal static void Line(StringBuilder builder, params string[] cells)
        => builder.AppendLine(string.Join(',', cells));
}
