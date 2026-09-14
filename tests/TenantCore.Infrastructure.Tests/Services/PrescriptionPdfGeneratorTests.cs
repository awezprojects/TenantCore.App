using System.Text;
using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Services;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Tests.Services;

public class PrescriptionPdfGeneratorTests
{
    private static readonly Guid ApplicationId = Guid.NewGuid();
    private readonly PrescriptionPdfGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_ReturnsPdfByteStream()
    {
        var (rx, patient, opd) = Build();

        var bytes = await _generator.GenerateAsync(rx, patient, opd);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task GenerateAsync_WhenNoMedicines_StillProducesPdf()
    {
        var (rx, patient, opd) = Build(items: []);

        var bytes = await _generator.GenerateAsync(rx, patient, opd);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task GenerateAsync_WhenCancelled_Throws()
    {
        var (rx, patient, opd) = Build();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _generator.GenerateAsync(rx, patient, opd, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static (Prescription Rx, Patient Patient, OpdRegistration Opd) Build(
        IReadOnlyList<PrescriptionItem>? items = null)
    {
        var patient = Patient.Create(
            ApplicationId, "Jane", "Doe", null, Gender.Female, "9999999999",
            "jane@example.com", null, null, null);

        var opd = OpdRegistration.Create(
            ApplicationId, patient.Id, Guid.NewGuid(), "Smith", "OPD-55", 250m, null,
            weight: 62.5m, bloodPressure: "120/80", pulseRate: 72);

        var rx = Prescription.Create(
            ApplicationId, opd.Id, patient.Id, Guid.NewGuid(), "Smith", "RX-1001",
            DateTime.UtcNow.AddDays(7), "Viral fever", "CBC", "Rest well",
            null, null, null, null, null, null, null,
            items ??
            [
                PrescriptionItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Paracetamol",
                    MedicineFormType.Tab, "500mg", "tablet", 1, 0, 1, 1, 5, 10,
                    null, null, null, "Take with water", null, null, 1)
            ]);

        return (rx, patient, opd);
    }
}
