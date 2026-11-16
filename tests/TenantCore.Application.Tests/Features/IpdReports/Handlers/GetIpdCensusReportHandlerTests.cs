using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Handlers;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdReports.Handlers;

public class GetIpdCensusReportHandlerTests
{
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    private GetIpdCensusReportHandler CreateHandler() => new(_bedRepository.Object, _accessValidator.Object);

    private static Bed BuildBed(Guid appId, Ward ward, Room room, bool occupied)
    {
        var bed = Bed.Create(appId, ward.Id, room.Id, Guid.NewGuid().ToString("N")[..6]);
        if (occupied) bed.MarkOccupied();
        typeof(Bed).GetProperty(nameof(Bed.Ward))!.SetValue(bed, ward);
        typeof(Bed).GetProperty(nameof(Bed.Room))!.SetValue(bed, room);
        return bed;
    }

    [Fact]
    public async Task Handle_MixedOccupancyAcrossWards_GroupsAndCountsCorrectly()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var wardA = Ward.Create(_appId, "Ward A", null);
        var wardB = Ward.Create(_appId, "Ward B", null);
        var roomA = Room.Create(_appId, wardA.Id, "101", "General", 500m);
        var roomB = Room.Create(_appId, wardB.Id, "201", "General", 500m);

        var beds = new[]
        {
            BuildBed(_appId, wardA, roomA, occupied: true),
            BuildBed(_appId, wardA, roomA, occupied: false),
            BuildBed(_appId, wardB, roomB, occupied: true),
        };
        _bedRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(beds);

        var result = await CreateHandler().Handle(new GetIpdCensusReportQuery(_appId), CancellationToken.None);

        result.TotalBeds.Should().Be(3);
        result.OccupiedBeds.Should().Be(2);
        result.AvailableBeds.Should().Be(1);
        result.Wards.Should().HaveCount(2);
        result.Wards.First(w => w.WardName == "Ward A").OccupiedBeds.Should().Be(1);
        result.Wards.First(w => w.WardName == "Ward B").OccupiedBeds.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InactiveBedsExcluded()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var ward = Ward.Create(_appId, "Ward A", null);
        var room = Room.Create(_appId, ward.Id, "101", "General", 500m);
        var active = BuildBed(_appId, ward, room, occupied: false);
        var inactive = BuildBed(_appId, ward, room, occupied: false);
        inactive.Deactivate();
        _bedRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync([active, inactive]);

        var result = await CreateHandler().Handle(new GetIpdCensusReportQuery(_appId), CancellationToken.None);

        result.TotalBeds.Should().Be(1);
    }
}
