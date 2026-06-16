using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Beds.Handlers;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Beds.Handlers;

public class GetBedsByRoomHandlerTests
{
    private readonly Mock<IBedRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsMappedBeds()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var bed1 = Bed.Create(applicationId, wardId, roomId, "B1");
        var bed2 = Bed.Create(applicationId, wardId, roomId, "B2");
        var query = new GetBedsByRoomQuery(roomId, applicationId);

        _repository.Setup(r => r.GetByRoomAsync(roomId, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { bed1, bed2 });

        var handler = new GetBedsByRoomHandler(_repository.Object);

        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(bed1.Id);
        result[0].WardId.Should().Be(wardId);
        result[0].RoomId.Should().Be(roomId);
        result[0].BedNumber.Should().Be("B1");
        result[1].Id.Should().Be(bed2.Id);
        result[1].BedNumber.Should().Be("B2");
    }
}
