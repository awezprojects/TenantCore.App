using System.Net;
using System.Text;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Prescriptions.Emails;

/// <summary>Subject, HTML body and attachment file name for the patient prescription email.</summary>
public sealed record PrescriptionEmailContent(string Subject, string HtmlBody, string AttachmentFileName);

/// <summary>
/// Builds the themed patient prescription email (appointment details + medicine summary).
/// Static and pure — same convention as the translators (never a mapping library).
/// </summary>
public static class PrescriptionEmailBuilder
{
    public static PrescriptionEmailContent Build(
        EmailTemplateTheme theme, Prescription prescription, Patient patient, OpdRegistration opd)
    {
        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();
        var subject = $"Your Prescription {prescription.PrescriptionNumber} — {patientName}";
        var attachment = $"Prescription-{SafeFilePart(prescription.PrescriptionNumber)}.pdf";
        return new PrescriptionEmailContent(subject, BuildHtml(theme, prescription, patient, opd), attachment);
    }

    private static string BuildHtml(
        EmailTemplateTheme theme, Prescription rx, Patient patient, OpdRegistration opd)
    {
        var t = EmailTemplateThemeCatalog.Get(theme);
        var headerBg = t.HeaderBackgroundEnd is null
            ? t.HeaderBackground
            : $"linear-gradient(90deg,{t.HeaderBackground} 0%,{t.HeaderBackgroundEnd} 100%)";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.Append("<meta name='viewport' content='width=device-width,initial-scale=1'/></head>");
        sb.Append($"<body style=\"margin:0;padding:24px;background:{t.BodyBackground};font-family:{t.FontFamily};color:{t.BodyText};\">");
        sb.Append("<table role='presentation' width='100%' cellpadding='0' cellspacing='0'><tr><td align='center'>");
        sb.Append($"<table role='presentation' cellpadding='0' cellspacing='0' style=\"width:100%;max-width:640px;background:{t.CardBackground};border:1px solid {t.CardBorder};border-radius:{t.CornerRadius}px;overflow:hidden;\">");

        // Header
        sb.Append($"<tr><td style=\"background:{headerBg};color:{t.HeaderText};padding:22px 28px;\">");
        sb.Append("<div style=\"font-size:12px;letter-spacing:2px;text-transform:uppercase;opacity:.85;\">Your Clinic</div>");
        sb.Append("<div style=\"font-size:22px;font-weight:700;margin-top:4px;\">Prescription</div>");
        sb.Append($"<div style=\"font-size:13px;opacity:.9;margin-top:2px;\">No. {E(rx.PrescriptionNumber)}</div>");
        sb.Append("</td></tr>");

        // Body
        sb.Append("<tr><td style=\"padding:24px 28px;\">");
        sb.Append($"<p style=\"margin:0 0 6px;font-size:15px;\">Dear {E(patient.FirstName)} {E(patient.LastName)},</p>");
        sb.Append($"<p style=\"margin:0 0 20px;font-size:13px;color:{t.MutedText};\">Your consultation with Dr. {E(rx.DoctorName)} on {rx.PrescribedDate:dd MMM yyyy} is complete. Your appointment details and medicines are summarised below; the full prescription is attached as a PDF.</p>");
        sb.Append(BuildAppointmentSection(t, opd));
        sb.Append(BuildMedicinesTable(t, rx));
        sb.Append(BuildFooter(t, rx));
        sb.Append("</td></tr></table></td></tr></table></body></html>");

        return sb.ToString();
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static string BuildAppointmentSection(EmailThemeTokens t, OpdRegistration opd)
    {
        var sb = new StringBuilder();
        sb.Append($"<div style=\"border:1px solid {t.CardBorder};border-radius:{t.CornerRadius}px;padding:14px 16px;margin-bottom:20px;background:{t.BodyBackground};\">");
        sb.Append($"<div style=\"font-size:12px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:{t.Accent};margin-bottom:8px;\">Appointment Details</div>");
        sb.Append("<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='font-size:13px;'>");
        sb.Append(Row(t, "Registration No.", E(opd.RegistrationNumber)));
        sb.Append(Row(t, "Visit Date", opd.RegistrationDate.ToString("dd MMM yyyy, hh:mm tt")));
        sb.Append(Row(t, "Doctor", "Dr. " + E(opd.DoctorName)));
        sb.Append(Row(t, "Consultation Fee", opd.Fee.ToString("0.##")));
        if (opd.Weight.HasValue) sb.Append(Row(t, "Weight", $"{opd.Weight.Value:0.##} kg"));
        if (!string.IsNullOrWhiteSpace(opd.BloodPressure)) sb.Append(Row(t, "Blood Pressure", E(opd.BloodPressure)));
        if (opd.PulseRate.HasValue) sb.Append(Row(t, "Pulse Rate", $"{opd.PulseRate.Value} bpm"));
        if (opd.OxygenSaturation.HasValue) sb.Append(Row(t, "SpO2", $"{opd.OxygenSaturation.Value:0.#} %"));
        if (opd.Temperature.HasValue) sb.Append(Row(t, "Temperature", $"{opd.Temperature.Value:0.#} F"));
        sb.Append("</table></div>");
        return sb.ToString();
    }

    private static string Row(EmailThemeTokens t, string label, string value) =>
        $"<tr><td style=\"padding:3px 0;color:{t.MutedText};width:150px;\">{label}</td>" +
        $"<td style=\"padding:3px 0;font-weight:600;color:{t.BodyText};\">{value}</td></tr>";

    private static string BuildMedicinesTable(EmailThemeTokens t, Prescription rx)
    {
        var border = $"border-top:1px solid {t.CardBorder};";
        var sb = new StringBuilder();
        sb.Append($"<div style=\"font-size:12px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:{t.Accent};margin-bottom:8px;\">Prescribed Medicines</div>");
        sb.Append($"<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style=\"border-collapse:collapse;font-size:12.5px;border:1px solid {t.CardBorder};border-radius:{t.CornerRadius}px;overflow:hidden;\">");
        sb.Append($"<tr style=\"background:{t.TableHeaderBackground};color:{t.TableHeaderText};\">");
        sb.Append("<th align='left' style='padding:8px 10px;'>Medicine</th>");
        sb.Append("<th align='left' style='padding:8px 10px;'>M-A-E-N</th>");
        sb.Append("<th align='left' style='padding:8px 10px;'>Duration</th>");
        sb.Append("<th align='left' style='padding:8px 10px;'>Qty</th>");
        sb.Append("<th align='left' style='padding:8px 10px;'>Remarks</th></tr>");

        if (rx.Items.Count == 0)
        {
            sb.Append($"<tr><td colspan='5' style=\"padding:10px;color:{t.MutedText};\">No medicines prescribed.</td></tr>");
        }
        else
        {
            var index = 0;
            foreach (var item in rx.Items.OrderBy(i => i.SortOrder))
            {
                var bg = index % 2 == 1 ? t.AlternateRowBackground : t.CardBackground;
                var strength = string.IsNullOrWhiteSpace(item.Strength)
                    ? string.Empty
                    : $" <span style=\"color:{t.MutedText};\">{E(item.Strength)}</span>";
                var dose = $"{item.DosageMorning ?? 0}-{item.DosageAfternoon ?? 0}-{item.DosageEvening ?? 0}-{item.DosageNight ?? 0}";
                sb.Append($"<tr style=\"background:{bg};\">");
                sb.Append($"<td style=\"padding:8px 10px;{border}\">{E(item.MedicineName)}{strength}</td>");
                sb.Append($"<td style=\"padding:8px 10px;{border}\">{dose}</td>");
                sb.Append($"<td style=\"padding:8px 10px;{border}\">{item.DurationDays} days</td>");
                sb.Append($"<td style=\"padding:8px 10px;{border}\">{item.Quantity:0.##} {E(item.DosageUnit)}</td>");
                sb.Append($"<td style=\"padding:8px 10px;{border}\">{E(item.RemarkEnglish)}</td></tr>");
                index++;
            }
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private static string BuildFooter(EmailThemeTokens t, Prescription rx)
    {
        var sb = new StringBuilder();
        if (rx.NextVisitDate.HasValue)
            sb.Append($"<p style=\"margin:18px 0 0;font-size:13px;\"><strong>Next visit:</strong> {rx.NextVisitDate.Value:dd MMM yyyy}</p>");
        if (!string.IsNullOrWhiteSpace(rx.Notes))
            sb.Append($"<p style=\"margin:10px 0 0;font-size:13px;\"><strong>Notes:</strong> {E(rx.Notes)}</p>");

        sb.Append($"<p style=\"margin:20px 0 0;padding-top:14px;border-top:1px solid {t.CardBorder};font-size:12px;color:{t.MutedText};\">Your full prescription is attached to this email as a PDF. Please carry it to your next visit. This message may contain confidential medical information intended only for the named patient.</p>");
        sb.Append($"<p style=\"margin:10px 0 0;font-size:12px;color:{t.MutedText};\">— Your Clinic</p>");
        return sb.ToString();
    }

    private static string SafeFilePart(string value)
    {
        var cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(cleaned) ? "Prescription" : cleaned;
    }
}
