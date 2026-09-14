using FluentAssertions;
using Moq;
using TenantCore.Application.Features.PrescriptionConfig.Commands;
using TenantCore.Application.Features.PrescriptionConfig.Handlers;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using Config = TenantCore.Domain.Entities.PrescriptionConfig;

namespace TenantCore.Application.Tests.Features.PrescriptionConfig.Handlers;

public class UpsertPrescriptionConfigHandlerTests
{
    private readonly Mock<IPrescriptionConfigRepository> _repository = new();
    private readonly UpsertPrescriptionConfigHandler _handler;

    public UpsertPrescriptionConfigHandlerTests()
        => _handler = new UpsertPrescriptionConfigHandler(_repository.Object);

    [Fact]
    public async Task Handle_WhenConfigMissing_CreatesRowWithSelectedEmailTheme()
    {
        _repository.Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Config?)null);

        var command = new UpsertPrescriptionConfigCommand(
            Guid.NewGuid(), PrescriptionLanguage.Marathi, 1, 2, 3, 4, true, EmailTemplateTheme.MidnightIndigo);

        var dto = await _handler.Handle(command, CancellationToken.None);

        dto.EmailTheme.Should().Be(EmailTemplateTheme.MidnightIndigo);
        _repository.Verify(r => r.AddAsync(
            It.Is<Config>(c => c.EmailTheme == EmailTemplateTheme.MidnightIndigo
                               && c.ApplicationId == command.ApplicationId),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenConfigExists_UpdatesEmailTheme()
    {
        var existing = Config.Create(
            Guid.NewGuid(), PrescriptionLanguage.English, emailTheme: EmailTemplateTheme.AzureClassic);
        _repository.Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existing);

        var command = new UpsertPrescriptionConfigCommand(
            existing.ApplicationId, PrescriptionLanguage.Hindi, 1, 2, 3, 4, false, EmailTemplateTheme.MinimalSlate);

        var dto = await _handler.Handle(command, CancellationToken.None);

        dto.EmailTheme.Should().Be(EmailTemplateTheme.MinimalSlate);
        existing.EmailTheme.Should().Be(EmailTemplateTheme.MinimalSlate);
        _repository.Verify(r => r.Update(existing), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Config>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlwaysPersistsChanges()
    {
        _repository.Setup(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Config?)null);

        var command = new UpsertPrescriptionConfigCommand(
            Guid.NewGuid(), PrescriptionLanguage.English, 0, 0, 0, 0, false, EmailTemplateTheme.EmeraldCare);

        await _handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
