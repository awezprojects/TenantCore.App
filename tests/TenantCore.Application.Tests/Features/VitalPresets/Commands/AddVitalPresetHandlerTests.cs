using FluentAssertions;
using Moq;
using TenantCore.Application.Features.VitalPresets.Commands;
using TenantCore.Application.Features.VitalPresets.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.VitalPresets.Commands;

public class AddVitalPresetHandlerTests
{
    private readonly Mock<IVitalPresetLookupItemRepository> _repository = new();

    [Fact]
    public async Task Handle_NewValue_CreatesClinicScopedItemAndSaves()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.FindAsync(VitalFieldType.Bp, appId, "125/82", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VitalPresetLookupItem?)null);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AddVitalPresetHandler(_repository.Object);
        var result = await handler.Handle(new AddVitalPresetCommand(appId, VitalFieldType.Bp, "125/82"), CancellationToken.None);

        result.Value.Should().Be("125/82");
        result.VitalField.Should().Be(VitalFieldType.Bp);
        result.IsGlobal.Should().BeFalse();
        _repository.Verify(r => r.AddAsync(It.Is<VitalPresetLookupItem>(v => v.ApplicationId == appId && v.Value == "125/82"), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValueAlreadyExists_ReusesExistingWithoutCreatingDuplicate()
    {
        var appId = Guid.NewGuid();
        var existing = VitalPresetLookupItem.CreateGlobal(VitalFieldType.Bp, "120/80");
        _repository.Setup(r => r.FindAsync(VitalFieldType.Bp, appId, "120/80", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new AddVitalPresetHandler(_repository.Object);
        var result = await handler.Handle(new AddVitalPresetCommand(appId, VitalFieldType.Bp, "120/80"), CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<VitalPresetLookupItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValueWithWhitespace_TrimsBeforeSaving()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.FindAsync(VitalFieldType.Sugar, appId, "95", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VitalPresetLookupItem?)null);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AddVitalPresetHandler(_repository.Object);
        var result = await handler.Handle(new AddVitalPresetCommand(appId, VitalFieldType.Sugar, "  95  "), CancellationToken.None);

        result.Value.Should().Be("95");
    }
}
