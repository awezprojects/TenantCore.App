using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.Rooms.Handlers;

public class UpdateRoomHandlerTests
{
    private readonly Mock<IRoomRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenRoomFoundAndNumberUnique_UpdatesAndReturnsDto()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var room = Room.Create(applicationId, wardId, "101", "Private", 250m);
        room.Beds.Add(Bed.Create(applicationId, wardId, room.Id, "B1"));
        var command = new UpdateRoomCommand(room.Id, applicationId, "201", "Semi-Private", 325m);

        _repository.Setup(r => r.GetByIdWithBedsAsync(room.Id, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _repository.Setup(r => r.ExistsByNumberAsync(wardId, command.RoomNumber, room.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateRoomHandler(_repository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(room.Id);
        result.RoomNumber.Should().Be(command.RoomNumber);
        result.RoomType.Should().Be(command.RoomType);
        result.PricePerDay.Should().Be(command.PricePerDay);
        result.BedCount.Should().Be(1);
        result.AvailableBeds.Should().Be(1);
        room.RoomNumber.Should().Be(command.RoomNumber);
        room.RoomType.Should().Be(command.RoomType);
        room.PricePerDay.Should().Be(command.PricePerDay);
        room.UpdatedAt.Should().NotBeNull();
        _repository.Verify(r => r.Update(room), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoomNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "201", "Semi-Private", 325m);
        _repository.Setup(r => r.GetByIdWithBedsAsync(command.Id, command.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = new UpdateRoomHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoomNumberAlreadyTaken_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var room = Room.Create(applicationId, wardId, "101", "Private", 250m);
        var command = new UpdateRoomCommand(room.Id, applicationId, "201", "Semi-Private", 325m);

        _repository.Setup(r => r.GetByIdWithBedsAsync(room.Id, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _repository.Setup(r => r.ExistsByNumberAsync(wardId, command.RoomNumber, room.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateRoomHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(UserMessages.RoomNumberTaken);
        _repository.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
