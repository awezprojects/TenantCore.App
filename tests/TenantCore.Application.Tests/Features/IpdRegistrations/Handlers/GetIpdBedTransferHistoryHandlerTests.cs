using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Handlers;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Handlers;

public class GetIpdBedTransferHistoryHandlerTests
{
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    private GetIpdBedTransferHistoryHandler CreateHandler() => new(_historyRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_HistoryExists_ReturnsMappedDtos()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var registrationId = Guid.NewGuid();
        var ward = Ward.Create(_appId, "Ward A", null);
        var room = Room.Create(_appId, ward.Id, "101", "General", 500m);
        var bed = Bed.Create(_appId, ward.Id, room.Id, "B1");
        var segment = IpdBedTransferHistory.Open(_appId, registrationId, bed.Id, room.Id, ward.Id, 500m, Guid.NewGuid(), "Initial");
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Bed))!.SetValue(segment, bed);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Room))!.SetValue(segment, room);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Ward))!.SetValue(segment, ward);

        _historyRepository.Setup(r => r.GetByRegistrationAsync(registrationId, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([segment]);

        var result = await CreateHandler().Handle(new GetIpdBedTransferHistoryQuery(registrationId, _appId), CancellationToken.None);

        result.Should().ContainSingle();
        result.First().BedId.Should().Be(bed.Id);
        result.First().WardName.Should().Be("Ward A");
        result.First().RoomNumber.Should().Be("101");
        result.First().BedNumber.Should().Be("B1");
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmptyList()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var registrationId = Guid.NewGuid();
        _historyRepository.Setup(r => r.GetByRegistrationAsync(registrationId, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetIpdBedTransferHistoryQuery(registrationId, _appId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AccessDenied_ThrowsUnauthorizedAccessException()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(false);

        var act = () => CreateHandler().Handle(new GetIpdBedTransferHistoryQuery(Guid.NewGuid(), _appId), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
