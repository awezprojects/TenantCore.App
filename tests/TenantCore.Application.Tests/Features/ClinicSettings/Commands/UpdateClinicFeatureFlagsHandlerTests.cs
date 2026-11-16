using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Commands;

public class UpdateClinicFeatureFlagsHandlerTests
{
    private readonly Mock<IClinicFeatureFlagsRepository> _repository = new();

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesPrepaidOpdEnabled()
    {
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(appId, false, true, null, null), CancellationToken.None);

        result.PrepaidOpdEnabled.Should().BeFalse();
        _repository.Verify(r => r.Update(flags), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesBillingEnabled()
    {
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(appId, true, false, null, null), CancellationToken.None);

        result.BillingEnabled.Should().BeFalse();
        flags.BillingEnabled.Should().BeFalse();
        _repository.Verify(r => r.Update(flags), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesIpdBillingSettings()
    {
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(
            appId, true, true, RoomBillingCycle.Per24HourBlock, MidStayBedChangeChargeMode.Prorated), CancellationToken.None);

        result.IpdRoomBillingCycle.Should().Be(RoomBillingCycle.Per24HourBlock);
        result.IpdMidStayChargeMode.Should().Be(MidStayBedChangeChargeMode.Prorated);
    }

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesShiftChargingSettings()
    {
        // DEF-IPD-16.
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(
            appId, true, true, null, null,
            IpdShiftGraceWindowMinutes: 90, IpdShiftGraceRule: IpdShiftGraceRule.OldRoomOnly,
            IpdManualShiftChargeMode: IpdManualShiftChargeMode.AdminAndReception,
            IpdFullDayShiftRateRule: IpdFullDayShiftRateRule.HigherRate), CancellationToken.None);

        result.IpdShiftGraceWindowMinutes.Should().Be(90);
        result.IpdShiftGraceRule.Should().Be(IpdShiftGraceRule.OldRoomOnly);
        result.IpdManualShiftChargeMode.Should().Be(IpdManualShiftChargeMode.AdminAndReception);
        result.IpdFullDayShiftRateRule.Should().Be(IpdFullDayShiftRateRule.HigherRate);
    }

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesCancelWindow()
    {
        // DEF-IPD-26 (15-Nov).
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(
            appId, true, true, null, null, IpdCancelWindowMinutes: 60), CancellationToken.None);

        result.IpdCancelWindowMinutes.Should().Be(60);
        flags.IpdCancelWindowMinutes.Should().Be(60);
        _repository.Verify(r => r.Update(flags), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoExistingFlags_CreatesWithCancelWindow()
    {
        // DEF-IPD-26 (15-Nov).
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(
            appId, false, false, null, null, IpdCancelWindowMinutes: 45), CancellationToken.None);

        result.IpdCancelWindowMinutes.Should().Be(45);
        _repository.Verify(r => r.AddAsync(It.Is<ClinicFeatureFlags>(f => f.IpdCancelWindowMinutes == 45), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoExistingFlags_CreatesThenReturnsUpdatedValue()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(appId, false, false, null, null), CancellationToken.None);

        result.PrepaidOpdEnabled.Should().BeFalse();
        result.BillingEnabled.Should().BeFalse();
        result.ApplicationId.Should().Be(appId);
        _repository.Verify(r => r.AddAsync(It.Is<ClinicFeatureFlags>(f => f.ApplicationId == appId && !f.PrepaidOpdEnabled && !f.BillingEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }
}
