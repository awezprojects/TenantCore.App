using FluentAssertions;
using Moq;
using TenantCore.Application.Features.VitalPresets.Commands;
using TenantCore.Application.Features.VitalPresets.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.VitalPresets.Commands;

public class DeleteVitalPresetHandlerTests
{
    private readonly Mock<IVitalPresetLookupItemRepository> _repository = new();

    [Fact]
    public async Task Handle_ClinicOwnedItem_DeletesAndSaves()
    {
        var appId = Guid.NewGuid();
        var item = VitalPresetLookupItem.CreateForClinic(appId, VitalFieldType.Bp, "125/82");
        _repository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteVitalPresetHandler(_repository.Object);
        await handler.Handle(new DeleteVitalPresetCommand(item.Id, appId), CancellationToken.None);

        _repository.Verify(r => r.Delete(item), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((VitalPresetLookupItem?)null);

        var handler = new DeleteVitalPresetHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteVitalPresetCommand(id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GlobalItem_ThrowsNotFoundException_CannotBeDeletedByAnyClinic()
    {
        var appId = Guid.NewGuid();
        var globalItem = VitalPresetLookupItem.CreateGlobal(VitalFieldType.Bp, "120/80");
        _repository.Setup(r => r.GetByIdAsync(globalItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(globalItem);

        var handler = new DeleteVitalPresetHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteVitalPresetCommand(globalItem.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<VitalPresetLookupItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemBelongsToDifferentClinic_ThrowsNotFoundException()
    {
        var ownerAppId = Guid.NewGuid();
        var requesterAppId = Guid.NewGuid();
        var item = VitalPresetLookupItem.CreateForClinic(ownerAppId, VitalFieldType.Bp, "125/82");
        _repository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var handler = new DeleteVitalPresetHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteVitalPresetCommand(item.Id, requesterAppId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
