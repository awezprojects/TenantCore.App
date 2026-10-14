using ClosedXML.Excel;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Export;

/// <summary>
/// Builds a multi-sheet Excel workbook from a pre-fetched <see cref="ClinicUsageDto"/>. The caller
/// already holds the data in memory (workspace rule 7) — nothing is re-queried here.
/// </summary>
internal static class UsageExcelExporter
{
    private static readonly string[] ItemLabels = ["OPD registration", "IPD admission", "IPD discharge", "OT surgery"];

    public static byte[] Build(ClinicUsageDto usage, string currency)
    {
        using var workbook = new XLWorkbook();

        AddSummarySheet(workbook, usage, currency);
        AddTotalsSheet(workbook, usage, currency);
        AddRecordsSheet(workbook, usage, currency);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSummarySheet(XLWorkbook workbook, ClinicUsageDto usage, string currency)
    {
        var ws = workbook.Worksheets.Add("Summary");
        ws.Style.Font.FontName = "Calibri";
        ws.Style.Font.FontSize = 11;

        ws.Cell("A1").Value = "CloudClinic — Usage export";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A3").Value = "Period from";
        ws.Cell("B3").Value = usage.From.ToLocalTime().ToString("dd MMM yyyy HH:mm");

        ws.Cell("A4").Value = "Period to";
        ws.Cell("B4").Value = usage.To.ToLocalTime().ToString("dd MMM yyyy HH:mm");

        ws.Cell("A6").Value = "Charged in range";
        ws.Cell("B6").Value = $"{currency} {usage.TotalCharged:N2}";

        ws.Cell("A7").Value = "Value at live rates";
        ws.Cell("B7").Value = $"{currency} {usage.TotalNotional:N2}";

        ws.Cell("A8").Value = "Total operations";
        ws.Cell("B8").Value = usage.Totals.Sum(t => t.Count);

        if (usage.IsCapped)
        {
            ws.Cell("A10").Value = "⚠ Note: the operations list is capped. Totals cover the full range.";
            ws.Cell("A10").Style.Font.FontColor = XLColor.DarkRed;
            ws.Range("A10:E10").Merge();
        }

        ws.Columns().AdjustToContents();
    }

    private static void AddTotalsSheet(XLWorkbook workbook, ClinicUsageDto usage, string currency)
    {
        var ws = workbook.Worksheets.Add("By operation type");
        ws.Style.Font.FontName = "Calibri";

        var headerRow = ws.Row(1);
        string[] headers = ["Operation", "Count", $"Charged ({currency})", $"At live rates ({currency})"];
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var tot in usage.Totals.OrderBy(t => (int)t.BillingItem))
        {
            ws.Cell(row, 1).Value = ItemLabel(tot.BillingItem);
            ws.Cell(row, 2).Value = tot.Count;
            ws.Cell(row, 3).Value = tot.Charged;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = tot.Notional;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static void AddRecordsSheet(XLWorkbook workbook, ClinicUsageDto usage, string currency)
    {
        var ws = workbook.Worksheets.Add("Operations");
        ws.Style.Font.FontName = "Calibri";

        string[] headers =
        [
            "Date (UTC)", "Operation", "Quantity", $"Unit rate ({currency})",
            $"Amount ({currency})", "Paid by", "Record ID"
        ];

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var record in usage.Records)
        {
            ws.Cell(row, 1).Value = record.ChargedAt.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = ItemLabel(record.BillingItem);
            ws.Cell(row, 3).Value = record.Quantity;
            ws.Cell(row, 4).Value = record.UnitRate;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.0000";
            ws.Cell(row, 5).Value = record.Amount;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = record.IsFree ? "Free" : "Wallet";
            ws.Cell(row, 7).Value = record.Id.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static string ItemLabel(BillingItem item) => item switch
    {
        BillingItem.OpdRegistration => ItemLabels[0],
        BillingItem.IpdAdmission    => ItemLabels[1],
        BillingItem.IpdDischarge    => ItemLabels[2],
        BillingItem.OtSurgery       => ItemLabels[3],
        _                           => item.ToString()
    };
}
