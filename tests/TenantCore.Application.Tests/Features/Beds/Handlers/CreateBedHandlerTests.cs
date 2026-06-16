using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Application.Features.Beds.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.Beds.Handlers;

public class CreateBedHandlerTests
{
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();

    [Fact]
    public async Task Handle_WhenRoomExistsAndBedNumberUnique_CreatesAndReturnsDto()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new CreateBedCommand(applicationId, roomId, "B1");
        var room = Room.Create(applicationId, wardId, "101", "Private", 250m);
        Bed? capturedBed = null;

        _roomRepository.Setup(r => r.GetByIdWithBedsAsync(roomId, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _bedRepository.Setup(r => r.ExistsByNumberAsync(roomId, command.BedNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _bedRepository.Setup(r => r.AddAsync(It.IsAny<Bed>(), It.IsAny<CancellationToken>()))
            .Callback<Bed, CancellationToken>((bed, _) => capturedBed = bed)
            .Returns(Task.CompletedTask);
        _bedRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateBedHandler(_bedRepository.Object, _roomRepository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(capturedBed!.Id);
        result.ApplicationId.Should().Be(applicationId);
        result.WardId.Should().Be(wardId);
        result.WardName.Should().BeEmpty();
        result.RoomId.Should().Be(roomId);
        result.RoomNumber.Should().BeEmpty();
        result.BedNumber.Should().Be(command.BedNumber);
        result.IsOccupied.Should().BeFalse();
        result.IsActive.Should().BeTrue();
        capturedBed.WardId.Should().Be(wardId);
        capturedBed.RoomId.Should().Be(roomId);
        capturedBed.BedNumber.Should().Be(command.BedNumber);
        _bedRepository.Verify(r => r.AddAsync(It.IsAny<Bed>(), It.IsAny<CancellationToken>()), Times.Once);
        _bedRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoomNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBedCommand(Guid.NewGuid(), Guid.NewGuid(), "B1");
        _roomRepository.Setup(r => r.GetByIdWithBedsAsync(command.RoomId, command.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = new CreateBedHandler(_bedRepository.Object, _roomRepository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _bedRepository.Verify(r => r.AddAsync(It.IsAny<Bed>(), It.IsAny<CancellationToken>()), Times.Never);
        _bedRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedNumberAlreadyExists_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new CreateBedCommand(applicationId, roomId, "B1");
        var room = Room.Create(applicationId, Guid.NewGuid(), "101", "Private", 250m);

        _roomRepository.Setup(r => r.GetByIdWithBedsAsync(roomId, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
        _bedRepository.Setup(r => r.ExistsByNumberAsync(roomId, command.BedNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateBedHandler(_bedRepository.Object, _roomRepository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(UserMessages.BedNumberTaken);
        _bedRepository.Verify(r => r.AddAsync(It.IsAny<Bed>(), It.IsAny<CancellationToken>()), Times.Never);
        _bedRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
