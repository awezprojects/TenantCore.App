using FluentAssertions;
using TenantCore.Application.Features.Prescriptions.Emails;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Prescriptions.Emails;

public class PrescriptionEmailBuilderTests
{
    private static readonly Guid ApplicationId = Guid.NewGuid();

    [Fact]
    public void Build_IncludesPatientAppointmentAndMedicineContent()
    {
        var patient = BuildPatient();
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id, BuildItem());

        var content = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);

        content.Subject.Should().Contain("RX-1001");
        content.Subject.Should().Contain("Jane Doe");
        content.HtmlBody.Should().Contain("Jane Doe");
        content.HtmlBody.Should().Contain("OPD-55");
        content.HtmlBody.Should().Contain("Dr. Smith");
        content.HtmlBody.Should().Contain("Appointment Details");
        content.HtmlBody.Should().Contain("Paracetamol");
        content.HtmlBody.Should().Contain("500mg");
    }

    [Fact]
    public void Build_AttachmentFileName_IsDerivedFromPrescriptionNumber()
    {
        var patient = BuildPatient();
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id, BuildItem());

        var content = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);

        content.AttachmentFileName.Should().Be("Prescription-RX1001.pdf");
    }

    [Fact]
    public void Build_WhenNextVisitAndNotesPresent_IncludesThem()
    {
        var patient = BuildPatient();
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id, BuildItem(),
            nextVisit: DateTime.UtcNow.AddDays(7), notes: "Drink plenty of water");

        var content = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);

        content.HtmlBody.Should().Contain("Next visit:");
        content.HtmlBody.Should().Contain("Drink plenty of water");
    }

    [Fact]
    public void Build_HtmlEncodesUntrustedFieldValues()
    {
        var patient = BuildPatient(firstName: "<script>alert(1)</script>");
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id, BuildItem());

        var content = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);

        content.HtmlBody.Should().NotContain("<script>");
        content.HtmlBody.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Build_WhenNoMedicines_RendersEmptyNotice()
    {
        var patient = BuildPatient();
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id);

        var content = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);

        content.HtmlBody.Should().Contain("No medicines prescribed.");
    }

    [Fact]
    public void Build_DifferentThemes_ProduceDifferentHtmlBodies()
    {
        var patient = BuildPatient();
        var opd = BuildOpd(patient.Id);
        var rx = BuildPrescription(patient.Id, opd.Id, BuildItem());

        var azure = PrescriptionEmailBuilder.Build(EmailTemplateTheme.AzureClassic, rx, patient, opd);
        var rose = PrescriptionEmailBuilder.Build(EmailTemplateTheme.SunsetRose, rx, patient, opd);

        azure.HtmlBody.Should().NotBe(rose.HtmlBody);
        azure.HtmlBody.Should().Contain("#1565C0");
        rose.HtmlBody.Should().Contain("#E11D48");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Patient BuildPatient(string firstName = "Jane", string lastName = "Doe") =>
        Patient.Create(
            ApplicationId, firstName, lastName, null, Gender.Female, "9999999999",
            "jane@example.com", null, null, null);

    private static OpdRegistration BuildOpd(Guid patientId) =>
        OpdRegistration.Create(
            ApplicationId, patientId, Guid.NewGuid(), "Smith", "OPD-55", 250m, null,
            weight: 62.5m, bloodPressure: "120/80", pulseRate: 72);

    private static PrescriptionItem BuildItem() =>
        PrescriptionItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Paracetamol", MedicineFormType.Tab, "500mg", "tablet",
            1, 0, 1, 1, 5, 10, "TDS", "After food", null, "Take with water", null, null, 1);

    private static Prescription BuildPrescription(
        Guid patientId, Guid opdId, PrescriptionItem? item = null,
        DateTime? nextVisit = null, string? notes = null) =>
        Prescription.Create(
            ApplicationId, opdId, patientId, Guid.NewGuid(), "Smith", "RX-1001",
            nextVisit, "Viral fever", "CBC", notes,
            null, null, null, null, null, null, null,
            item is null ? [] : [item]);
}
