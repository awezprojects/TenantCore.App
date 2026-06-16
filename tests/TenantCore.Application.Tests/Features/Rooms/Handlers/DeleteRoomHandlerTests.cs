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
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

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

        var handler = new DeleteRoomHandler(_repository.Object, _accessValidator.Object);

        await handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(room), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoomNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = new DeleteRoomHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

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

        var handler = new DeleteRoomHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
