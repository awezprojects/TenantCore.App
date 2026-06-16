using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Application.Features.Wards.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class GetWardByIdHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenWardFound_ReturnsMappedDto()
    {
        var applicationId = Guid.NewGuid();
        var ward = CreateWardWithRooms(applicationId);
        var query = new GetWardByIdQuery(ward.Id, applicationId);

        _repository.Setup(r => r.GetByIdWithRoomsAsync(query.Id, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);

        var handler = new GetWardByIdHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(ward.Id);
        result.Name.Should().Be(ward.Name);
        result.RoomCount.Should().Be(1);
        result.BedCount.Should().Be(2);
        result.AvailableBeds.Should().Be(1);
        result.Rooms.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenWardNotFound_ThrowsNotFoundException()
    {
        var query = new GetWardByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        _repository.Setup(r => r.GetByIdWithRoomsAsync(query.Id, query.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ward?)null);

        var handler = new GetWardByIdHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static Ward CreateWardWithRooms(Guid applicationId)
    {
        var ward = Ward.Create(applicationId, "General Ward", "General patients");
        var room = Room.Create(applicationId, ward.Id, "101", "Private", 1500m);
        var availableBed = Bed.Create(applicationId, ward.Id, room.Id, "B1");
        var occupiedBed = Bed.Create(applicationId, ward.Id, room.Id, "B2");
        occupiedBed.MarkOccupied();

        SetProperty(room, nameof(Room.Ward), ward);
        SetProperty(availableBed, nameof(Bed.Ward), ward);
        SetProperty(availableBed, nameof(Bed.Room), room);
        SetProperty(occupiedBed, nameof(Bed.Ward), ward);
        SetProperty(occupiedBed, nameof(Bed.Room), room);

        room.Beds.Add(availableBed);
        room.Beds.Add(occupiedBed);
        ward.Rooms.Add(room);

        return ward;
    }

    private static void SetProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        typeof(TTarget)
            .GetProperty(propertyName)!
            .SetValue(target, value);
    }
}
