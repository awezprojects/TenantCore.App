using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Rooms.Handlers;
using TenantCore.Application.Features.Rooms.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Rooms.Handlers;

public class GetRoomsByWardHandlerTests
{
    private readonly Mock<IRoomRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsMappedRooms()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var room1 = Room.Create(applicationId, wardId, "101", "Private", 250m);
        var room2 = Room.Create(applicationId, wardId, "102", "General", 150m);
        room1.Beds.Add(Bed.Create(applicationId, wardId, room1.Id, "B1"));
        room1.Beds.Add(Bed.Create(applicationId, wardId, room1.Id, "B2"));
        var occupiedBed = Bed.Create(applicationId, wardId, room2.Id, "B3");
        occupiedBed.MarkOccupied();
        room2.Beds.Add(occupiedBed);

        _repository.Setup(r => r.GetByWardAsync(wardId, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { room1, room2 });

        var handler = new GetRoomsByWardHandler(_repository.Object);
        var query = new GetRoomsByWardQuery(wardId, applicationId);

        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(room1.Id);
        result[0].RoomNumber.Should().Be("101");
        result[0].BedCount.Should().Be(2);
        result[0].AvailableBeds.Should().Be(2);
        result[1].Id.Should().Be(room2.Id);
        result[1].RoomNumber.Should().Be("102");
        result[1].BedCount.Should().Be(1);
        result[1].AvailableBeds.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNoRooms_ReturnsEmptyCollection()
    {
        var query = new GetRoomsByWardQuery(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByWardAsync(query.WardId, query.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Room>());

        var handler = new GetRoomsByWardHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
