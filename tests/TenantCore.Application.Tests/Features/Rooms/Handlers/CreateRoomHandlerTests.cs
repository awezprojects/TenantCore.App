using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Rooms.Commands;
using TenantCore.Application.Features.Rooms.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.Rooms.Handlers;

public class CreateRoomHandlerTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IWardRepository> _wardRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenWardExistsAndRoomNumberUnique_CreatesAndReturnsDto()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var command = new CreateRoomCommand(applicationId, wardId, "101", "Private", 250m);
        var ward = Ward.Create(applicationId, "Ward A", "General ward");
        Room? capturedRoom = null;

        _wardRepository.Setup(r => r.GetByIdAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _roomRepository.Setup(r => r.ExistsByNumberAsync(wardId, command.RoomNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roomRepository.Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback<Room, CancellationToken>((room, _) => capturedRoom = room)
            .Returns(Task.CompletedTask);
        _roomRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _roomRepository.Setup(r => r.GetByIdWithBedsAsync(It.IsAny<Guid>(), applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedRoom);

        var handler = new CreateRoomHandler(_roomRepository.Object, _wardRepository.Object, _accessValidator.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(capturedRoom!.Id);
        result.ApplicationId.Should().Be(applicationId);
        result.WardId.Should().Be(wardId);
        result.RoomNumber.Should().Be(command.RoomNumber);
        result.RoomType.Should().Be(command.RoomType);
        result.PricePerDay.Should().Be(command.PricePerDay);
        result.BedCount.Should().Be(0);
        result.AvailableBeds.Should().Be(0);
        capturedRoom.ApplicationId.Should().Be(applicationId);
        capturedRoom.WardId.Should().Be(wardId);
        capturedRoom.RoomNumber.Should().Be(command.RoomNumber);
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Once);
        _roomRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _roomRepository.Verify(r => r.GetByIdWithBedsAsync(capturedRoom.Id, applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWardNotFound_ThrowsNotFoundException()
    {
        var command = new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", "Private", 250m);
        _wardRepository.Setup(r => r.GetByIdAsync(command.WardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ward?)null);

        var handler = new CreateRoomHandler(_roomRepository.Object, _wardRepository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var command = new CreateRoomCommand(applicationId, wardId, "101", "Private", 250m);
        var ward = Ward.Create(applicationId, "Ward A", "General ward");

        _wardRepository.Setup(r => r.GetByIdAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(false);

        var handler = new CreateRoomHandler(_roomRepository.Object, _wardRepository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoomNumberAlreadyExists_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var command = new CreateRoomCommand(applicationId, wardId, "101", "Private", 250m);
        var ward = Ward.Create(applicationId, "Ward A", "General ward");

        _wardRepository.Setup(r => r.GetByIdAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _roomRepository.Setup(r => r.ExistsByNumberAsync(wardId, command.RoomNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateRoomHandler(_roomRepository.Object, _wardRepository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(UserMessages.RoomNumberTaken);
        _roomRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
