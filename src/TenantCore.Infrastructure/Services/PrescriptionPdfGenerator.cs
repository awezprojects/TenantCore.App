using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Renders a complete prescription to an A4 PDF with QuestPDF — used as the
/// attachment on the patient prescription email. Layout mirrors the client print page
/// (clinic header, patient strip, appointment details, medicines table, signature, disclaimer).
/// </summary>
public class PrescriptionPdfGenerator : IPrescriptionPdfGenerator
{
    private static readonly Color Primary = Color.FromHex("#1565C0");
    private static readonly Color Border = Color.FromHex("#CBD5E1");
    private static readonly Color Muted = Color.FromHex("#64748B");
    private static readonly Color Band = Color.FromHex("#EFF6FF");
    private static readonly Color White = Color.FromHex("#FFFFFF");
    private static readonly Color Ink = Color.FromHex("#0F172A");

    static PrescriptionPdfGenerator()
    {
        // Free for organizations under $1M USD annual gross revenue.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateAsync(
        Prescription prescription,
        Patient patient,
        OpdRegistration opdRegistration,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));

                page.Header().Element(c => ComposeHeader(c, prescription));
                page.Content().Element(c => ComposeContent(c, prescription, patient, opdRegistration));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void ComposeHeader(IContainer container, Prescription rx)
    {
        container.BorderBottom(2).BorderColor(Primary).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Your Clinic").FontSize(17).Bold().FontColor(Primary);
                col.Item().Text("Prescription").FontSize(11).FontColor(Muted);
            });

            row.ConstantItem(190).AlignRight().Column(col =>
            {
                col.Item().Text($"No. {rx.PrescriptionNumber}").FontSize(10).SemiBold();
                col.Item().Text(rx.PrescribedDate.ToString("dd MMM yyyy")).FontSize(9).FontColor(Muted);
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Border).PaddingTop(6).Row(row =>
        {
            row.RelativeItem()
                .Text("This prescription is confidential and intended only for the named patient.")
                .FontSize(8).FontColor(Muted);

            row.ConstantItem(110).AlignRight().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static void ComposeContent(
        IContainer container, Prescription rx, Patient patient, OpdRegistration opd)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(10);

            // Patient strip
            col.Item().Background(Band).Border(1).BorderColor(Border).Padding(8).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Patient: ").SemiBold();
                    t.Span($"{patient.FirstName} {patient.LastName}");
                });
                row.ConstantItem(200).AlignRight().Text(t =>
                {
                    t.Span("Phone: ").SemiBold();
                    t.Span(patient.PhoneNumber);
                });
            });

            // Appointment details
            col.Item().Text("Appointment Details").FontSize(11).Bold().FontColor(Primary);
            col.Item().Border(1).BorderColor(Border).Padding(8).Column(c =>
            {
                c.Spacing(3);
                c.Item().Text($"Registration No.: {opd.RegistrationNumber}");
                c.Item().Text($"Visit Date: {opd.RegistrationDate:dd MMM yyyy, hh:mm tt}");
                c.Item().Text($"Doctor: Dr. {opd.DoctorName}");
                c.Item().Text($"Consultation Fee: {opd.Fee:0.##}");
                if (opd.Weight.HasValue) c.Item().Text($"Weight: {opd.Weight.Value:0.##} kg");
                if (!string.IsNullOrWhiteSpace(opd.BloodPressure)) c.Item().Text($"Blood Pressure: {opd.BloodPressure}");
                if (opd.PulseRate.HasValue) c.Item().Text($"Pulse Rate: {opd.PulseRate.Value} bpm");
                if (opd.OxygenSaturation.HasValue) c.Item().Text($"SpO2: {opd.OxygenSaturation.Value:0.#} %");
                if (opd.Temperature.HasValue) c.Item().Text($"Temperature: {opd.Temperature.Value:0.#} F");
            });

            if (!string.IsNullOrWhiteSpace(rx.Diagnosis))
                col.Item().Text(t => { t.Span("Diagnosis: ").SemiBold(); t.Span(rx.Diagnosis!); });
            if (!string.IsNullOrWhiteSpace(rx.Investigations))
                col.Item().Text(t => { t.Span("Investigations: ").SemiBold(); t.Span(rx.Investigations!); });

            // Medicines
            col.Item().Text("Prescribed Medicines").FontSize(11).Bold().FontColor(Primary);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(2.5f);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Medicine");
                    HeaderCell(header, "M-A-E-N");
                    HeaderCell(header, "Duration");
                    HeaderCell(header, "Qty");
                    HeaderCell(header, "Remarks");
                });

                if (rx.Items.Count == 0)
                {
                    table.Cell().ColumnSpan(5).Padding(6).Text("No medicines prescribed.").FontColor(Muted);
                }
                else
                {
                    foreach (var item in rx.Items.OrderBy(i => i.SortOrder))
                    {
                        var strength = string.IsNullOrWhiteSpace(item.Strength) ? string.Empty : $" {item.Strength}";
                        var dose = $"{item.DosageMorning ?? 0}-{item.DosageAfternoon ?? 0}-{item.DosageEvening ?? 0}-{item.DosageNight ?? 0}";
                        BodyCell(table, $"{item.MedicineName}{strength}");
                        BodyCell(table, dose);
                        BodyCell(table, $"{item.DurationDays} days");
                        BodyCell(table, $"{item.Quantity:0.##} {item.DosageUnit}");
                        BodyCell(table, item.RemarkEnglish ?? string.Empty);
                    }
                }
            });

            if (rx.NextVisitDate.HasValue)
                col.Item().Text($"Next visit: {rx.NextVisitDate.Value:dd MMM yyyy}").SemiBold();
            if (!string.IsNullOrWhiteSpace(rx.Notes))
                col.Item().Text(t => { t.Span("Notes: ").SemiBold(); t.Span(rx.Notes!); });

            // Signature
            col.Item().PaddingTop(28).AlignRight().Column(c =>
            {
                c.Item().Text("_________________________").FontColor(Muted);
                c.Item().Text($"Dr. {rx.DoctorName}").SemiBold();
                c.Item().Text("Signature").FontSize(8).FontColor(Muted);
            });
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text) =>

        header.Cell().Background(Primary).PaddingVertical(5).PaddingHorizontal(6)
            .Text(text).FontColor(White).SemiBold();

    private static void BodyCell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(1).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(6).Text(text);
}
