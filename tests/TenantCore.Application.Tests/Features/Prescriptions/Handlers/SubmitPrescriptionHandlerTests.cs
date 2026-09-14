using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Handlers;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using Config = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Tests.Features.Prescriptions.Handlers;

public class SubmitPrescriptionHandlerTests
{
    private static readonly Guid ApplicationId = Guid.NewGuid();

    private readonly Mock<IPrescriptionRepository> _prescriptionRepo = new();
    private readonly Mock<IOpdRegistrationRepository> _opdRepo = new();
    private readonly Mock<IPatientRepository> _patientRepo = new();
    private readonly Mock<IPrescriptionConfigRepository> _configRepo = new();
    private readonly Mock<IPrescriptionPdfGenerator> _pdfGenerator = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly SubmitPrescriptionHandler _handler;

    private string? _capturedHtml;
    private byte[]? _capturedPdf;
    private string? _capturedAttachmentName;

    public SubmitPrescriptionHandlerTests()
    {
        _handler = new SubmitPrescriptionHandler(
            _prescriptionRepo.Object, _opdRepo.Object, _patientRepo.Object,
            _configRepo.Object, _pdfGenerator.Object, _emailService.Object,
            new Mock<ILogger<SubmitPrescriptionHandler>>().Object, _accessValidator.Object);

        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);

        _pdfGenerator
            .Setup(g => g.GenerateAsync(It.IsAny<Prescription>(), It.IsAny<Patient>(),
                It.IsAny<OpdRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3, 4]);

        _emailService
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, byte[]?, string?, CancellationToken>((to, subject, html, pdf, name, ct) =>
            {
                _capturedHtml = html;
                _capturedPdf = pdf;
                _capturedAttachmentName = name;
            })
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_WithPatientEmail_SendsEmailWithPdfAttachmentAndThemedBody()
    {
        var (rx, _, _) = Arrange(patientEmail: "jane@example.com", theme: EmailTemplateTheme.SunsetRose);

        await _handler.Handle(new SubmitPrescriptionCommand(rx.Id, ApplicationId), CancellationToken.None);

        _capturedPdf.Should().NotBeNull();
        _capturedPdf!.Length.Should().Be(4);
        _capturedAttachmentName.Should().Be("Prescription-RX1001.pdf");
        _capturedHtml.Should().NotBeNull();
        _capturedHtml!.Should().Contain("#E11D48");   // theme resolved from clinic config
        _capturedHtml.Should().Contain("OPD-55");     // appointment details rendered
        _capturedHtml.Should().Contain("Paracetamol");
        rx.IsEmailSent.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenConfigMissing_DefaultsToAzureClassicTheme()
    {
        var (rx, _, _) = Arrange(patientEmail: "jane@example.com", theme: null);

        await _handler.Handle(new SubmitPrescriptionCommand(rx.Id, ApplicationId), CancellationToken.None);

        _capturedHtml!.Should().Contain("#1565C0");
    }

    [Fact]
    public async Task Handle_WhenPatientHasNoEmail_SkipsEmailAndPdfGeneration()
    {
        var (rx, _, _) = Arrange(patientEmail: null, theme: EmailTemplateTheme.AzureClassic);

        await _handler.Handle(new SubmitPrescriptionCommand(rx.Id, ApplicationId), CancellationToken.None);

        _emailService.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _pdfGenerator.Verify(g => g.GenerateAsync(It.IsAny<Prescription>(), It.IsAny<Patient>(),
            It.IsAny<OpdRegistration>(), It.IsAny<CancellationToken>()), Times.Never);
        rx.IsEmailSent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenEmailSendFails_DoesNotThrowAndMarksNotSent()
    {
        var (rx, _, _) = Arrange(patientEmail: "jane@example.com", theme: EmailTemplateTheme.AzureClassic);
        _emailService
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var act = async () => await _handler.Handle(
            new SubmitPrescriptionCommand(rx.Id, ApplicationId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        rx.IsEmailSent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPrescriptionNotFound_ThrowsNotFoundException()
    {
        _prescriptionRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Prescription?)null);

        var act = async () => await _handler.Handle(
            new SubmitPrescriptionCommand(Guid.NewGuid(), ApplicationId), CancellationToken.None);

        await act.Should().ThrowAsync<TenantCore.Domain.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserCannotAccessClinic_ThrowsNotFoundException()
    {
        var (rx, _, _) = Arrange(patientEmail: "jane@example.com", theme: EmailTemplateTheme.AzureClassic);
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(false);

        var act = async () => await _handler.Handle(
            new SubmitPrescriptionCommand(rx.Id, ApplicationId), CancellationToken.None);

        await act.Should().ThrowAsync<TenantCore.Domain.Exceptions.NotFoundException>();
    }

    private (Prescription Rx, Patient Patient, OpdRegistration Opd) Arrange(string? patientEmail, EmailTemplateTheme? theme)
    {
        var patient = Patient.Create(
            ApplicationId, "Jane", "Doe", null, Gender.Female, "9999999999",
            patientEmail, null, null, null);

        var opd = OpdRegistration.Create(
            ApplicationId, patient.Id, Guid.NewGuid(), "Smith", "OPD-55", 250m, null);

        var rx = Prescription.Create(
            ApplicationId, opd.Id, patient.Id, Guid.NewGuid(), "Smith", "RX-1001",
            null, "Viral fever", null, null, null, null, null, null, null, null, null,
            [PrescriptionItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Paracetamol",
                MedicineFormType.Tab, "500mg", "tablet", 1, 0, 1, 1, 5, 10,
                null, null, null, null, null, null, 1)]);

        _prescriptionRepo.Setup(r => r.GetByIdWithDetailsAsync(rx.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(rx);
        _patientRepo.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(patient);
        _opdRepo.Setup(r => r.GetByIdAsync(opd.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(opd);

        _configRepo.Setup(r => r.GetByApplicationIdAsync(ApplicationId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(theme is null
                       ? null
                       : Config.Create(ApplicationId, PrescriptionLanguage.English, emailTheme: theme.Value));

        return (rx, patient, opd);
    }
}
