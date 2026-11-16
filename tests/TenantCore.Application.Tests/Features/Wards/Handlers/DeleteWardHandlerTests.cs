using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class DeleteWardHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    public DeleteWardHandlerTests()
    {
        _historyRepository.Setup(h => h.HasAnyForWardAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
    }

    private DeleteWardHandler CreateHandler() => new(_repository.Object, _historyRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_WhenWardFoundAndAccessGranted_DeletesWard()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId))
            .Returns(true);

        await CreateHandler().Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(ward), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWardNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteWardCommand(Guid.NewGuid(), Guid.NewGuid());

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ward?)null);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId))
            .Returns(false);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerLacksAdminRoleInWardsClinic_ThrowsNotFoundException()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId)).Returns(true);
        _accessValidator.Setup(v => v.HasRole(ward.ApplicationId, It.IsAny<IEnumerable<string>>())).Returns(false);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWardHasOccupiedBed_ThrowsDomainValidationException()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var room = Room.Create(ward.ApplicationId, ward.Id, "101", "Private", 250m);
        var bed = Bed.Create(ward.ApplicationId, ward.Id, room.Id, "B1");
        bed.MarkOccupied();
        room.Beds.Add(bed);
        ward.Rooms.Add(room);
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId)).Returns(true);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("Cannot delete a ward with an occupied bed.");
        _repository.Verify(r => r.Delete(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWardHasAdmissionHistory_ThrowsInvalidOperationException()
    {
        // DEF-IPD-17 — see DeleteBedHandlerTests for the same fix one level down.
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId)).Returns(true);
        _historyRepository.Setup(h => h.HasAnyForWardAsync(ward.Id, ward.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.Delete(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
