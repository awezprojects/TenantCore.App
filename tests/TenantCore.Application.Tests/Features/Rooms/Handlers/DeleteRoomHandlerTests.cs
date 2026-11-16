using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Rooms.Handlers;

public class DeleteRoomHandlerTests
{
    private readonly Mock<IRoomRepository> _repository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    public DeleteRoomHandlerTests()
    {
        _historyRepository.Setup(h => h.HasAnyForRoomAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
    }

    private DeleteRoomHandler CreateHandler() => new(_repository.Object, _historyRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_WhenRoomFoundAndAccessGranted_DeletesRoom()
    {
        var applicationId = Guid.NewGuid();
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);
        var command = new DeleteRoomCommand(room.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateHandler().Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(room), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoomNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var applicationId = Guid.NewGuid();
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);
        var command = new DeleteRoomCommand(room.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(false);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerLacksAdminRoleInRoomsClinic_ThrowsNotFoundException()
    {
        var applicationId = Guid.NewGuid();
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);
        var command = new DeleteRoomCommand(room.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _accessValidator.Setup(v => v.HasRole(applicationId, It.IsAny<IEnumerable<string>>())).Returns(false);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoomHasOccupiedBed_ThrowsDomainValidationException()
    {
        var applicationId = Guid.NewGuid();
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);
        var bed = Bed.Create(applicationId, Guid.NewGuid(), room.Id, "B1");
        bed.MarkOccupied();
        room.Beds.Add(bed);
        var command = new DeleteRoomCommand(room.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("Cannot delete a room with an occupied bed.");
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoomHasAdmissionHistory_ThrowsInvalidOperationException()
    {
        // DEF-IPD-17 — see DeleteBedHandlerTests for the same fix one level up.
        var applicationId = Guid.NewGuid();
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);
        var command = new DeleteRoomCommand(room.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _historyRepository.Setup(h => h.HasAnyForRoomAsync(room.Id, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
