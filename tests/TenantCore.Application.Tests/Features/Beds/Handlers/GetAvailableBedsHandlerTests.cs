using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Beds.Handlers;
using TenantCore.Application.Features.Beds.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Beds.Handlers;

public class GetAvailableBedsHandlerTests
{
    private readonly Mock<IBedRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenWardIdProvided_GetsAvailableByWard()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var beds = new[]
        {
            Bed.Create(applicationId, wardId, roomId, "B1"),
            Bed.Create(applicationId, wardId, roomId, "B2")
        };
        var query = new GetAvailableBedsQuery(applicationId, wardId);

        _repository.Setup(r => r.GetAvailableByWardAsync(wardId, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beds);

        var handler = new GetAvailableBedsHandler(_repository.Object);

        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Select(x => x.BedNumber).Should().BeEquivalentTo(["B1", "B2"]);
        _repository.Verify(r => r.GetAvailableByWardAsync(wardId, applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWardIdNotProvided_GetsAllAndFilters()
    {
        var applicationId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var availableBed = Bed.Create(applicationId, wardId, roomId, "B1");
        var occupiedBed = Bed.Create(applicationId, wardId, roomId, "B2");
        occupiedBed.MarkOccupied();
        var inactiveBed = Bed.Create(applicationId, wardId, roomId, "B3");
        inactiveBed.Deactivate();
        var query = new GetAvailableBedsQuery(applicationId);

        _repository.Setup(r => r.GetByApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { availableBed, occupiedBed, inactiveBed });

        var handler = new GetAvailableBedsHandler(_repository.Object);

        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(availableBed.Id);
        result[0].BedNumber.Should().Be("B1");
        _repository.Verify(r => r.GetByApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetAvailableByWardAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
