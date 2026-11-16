using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdRegistrations.Handlers;
using TenantCore.Application.Features.IpdRegistrations.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Handlers;

public class PreviewTransferBedHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly IIpdBillCalculator _billCalculator = new IpdBillCalculator(
        Mock.Of<IIpdBedTransferHistoryRepository>(), Mock.Of<IClinicFeatureFlagsRepository>());
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    private readonly Guid _appId = Guid.NewGuid();

    public PreviewTransferBedHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
    }

    private PreviewTransferBedHandler CreateHandler() => new(
        _ipdRepository.Object, _bedRepository.Object, _historyRepository.Object,
        _featureFlagsRepository.Object, _billCalculator, _accessValidator.Object);

    private IpdRegistration BuildAdmittedRegistration(Guid? currentBedId)
        => IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1",
            currentBedId, "Ward A", "101", "B1", 0m, null);

    private static Bed BuildBed(Guid applicationId, decimal rate, string bedNumber = "B2")
    {
        var ward = Ward.Create(applicationId, "Ward A", null);
        var room = Room.Create(applicationId, ward.Id, "101", "General", rate);
        var bed = Bed.Create(applicationId, ward.Id, room.Id, bedNumber);
        typeof(Bed).GetProperty(nameof(Bed.Ward))!.SetValue(bed, ward);
        typeof(Bed).GetProperty(nameof(Bed.Room))!.SetValue(bed, room);
        return bed;
    }

    private ClinicFeatureFlags ConfiguredFlags(
        int graceWindow = 0, IpdShiftGraceRule? graceRule = null, IpdManualShiftChargeMode manualMode = IpdManualShiftChargeMode.Off)
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(graceWindow, graceRule, manualMode, IpdFullDayShiftRateRule.DestinationRoomRate);
        return flags;
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsNotFoundException()
    {
        _ipdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((IpdRegistration?)null);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(Guid.NewGuid(), _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CallerLacksAccess_ThrowsUnauthorizedAccessException()
    {
        var registration = BuildAdmittedRegistration(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _accessValidator.Setup(v => v.CanAccess(registration.ApplicationId)).Returns(false);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_CallerLacksReceptionRoleInRegistrationsClinic_ThrowsUnauthorizedAccessException()
    {
        var registration = BuildAdmittedRegistration(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _accessValidator.Setup(v => v.HasRole(_appId, It.IsAny<IEnumerable<string>>())).Returns(false);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_BillingSettingsNotConfigured_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicFeatureFlags?)null);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_BedBelongsToAnotherClinic_ThrowsNotFoundException()
    {
        // SECURITY regression: must not disclose another clinic's room rate, and must not serve
        // as a bed-id enumeration oracle.
        var registration = BuildAdmittedRegistration(null);
        var otherClinicBed = BuildBed(Guid.NewGuid(), 2500m); // different ApplicationId
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(ConfiguredFlags());
        _bedRepository.Setup(r => r.GetByIdAsync(otherClinicBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherClinicBed);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, otherClinicBed.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BedIsInactive_ThrowsNotFoundException()
    {
        var registration = BuildAdmittedRegistration(null);
        var inactiveBed = BuildBed(_appId, 500m);
        inactiveBed.Deactivate();
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(ConfiguredFlags());
        _bedRepository.Setup(r => r.GetByIdAsync(inactiveBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(inactiveBed);

        var act = () => CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, inactiveBed.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoCurrentSegment_ReturnsZeroedPreview()
    {
        // Ward-only admission allocating its first bed — not a shift, nothing to preview.
        var registration = BuildAdmittedRegistration(null);
        var newBed = BuildBed(_appId, 500m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(ConfiguredFlags());
        _bedRepository.Setup(r => r.GetByIdAsync(newBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);

        var result = await CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, newBed.Id), CancellationToken.None);

        result.StandardAmount.Should().Be(0m);
        result.WithinGraceWindow.Should().BeFalse();
        result.ManualChargeAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithinGraceWindow_SuggestsGraceAdjustedAmount()
    {
        var oldBed = BuildBed(_appId, 2500m, "ICU-1");
        var registration = BuildAdmittedRegistration(oldBed.Id);
        var currentSegment = IpdBedTransferHistory.Open(_appId, registration.Id, oldBed.Id, oldBed.RoomId, oldBed.WardId, 2500m, Guid.NewGuid(), null);
        var newBed = BuildBed(_appId, 300m, "GEN-1");

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags(120, IpdShiftGraceRule.NewRoomOnly));
        _bedRepository.Setup(r => r.GetByIdAsync(newBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSegment);

        var result = await CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, newBed.Id), CancellationToken.None);

        result.WithinGraceWindow.Should().BeTrue();
        result.StandardAmount.Should().Be(2500m);
        result.SuggestedAmount.Should().Be(300m);
        result.MaxManualAmount.Should().Be(2500m);
    }

    [Fact]
    public async Task Handle_ManualChargeModeOff_ManualChargeAllowedIsFalse()
    {
        var oldBed = BuildBed(_appId, 2500m, "ICU-1");
        var registration = BuildAdmittedRegistration(oldBed.Id);
        var currentSegment = IpdBedTransferHistory.Open(_appId, registration.Id, oldBed.Id, oldBed.RoomId, oldBed.WardId, 2500m, Guid.NewGuid(), null);
        var newBed = BuildBed(_appId, 300m, "GEN-1");

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(ConfiguredFlags());
        _bedRepository.Setup(r => r.GetByIdAsync(newBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSegment);

        var result = await CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, newBed.Id), CancellationToken.None);

        result.ManualChargeAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ManualChargeModeAdminOnly_ChecksRoleAgainstRegistrationsClinic()
    {
        var oldBed = BuildBed(_appId, 2500m, "ICU-1");
        var registration = BuildAdmittedRegistration(oldBed.Id);
        var currentSegment = IpdBedTransferHistory.Open(_appId, registration.Id, oldBed.Id, oldBed.RoomId, oldBed.WardId, 2500m, Guid.NewGuid(), null);
        var newBed = BuildBed(_appId, 300m, "GEN-1");

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags(manualMode: IpdManualShiftChargeMode.AdminOnly));
        _bedRepository.Setup(r => r.GetByIdAsync(newBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSegment);
        // Exact single-role match — the broader reception-role gate checked earlier in Handle
        // also includes "Clinic Admin" in its role list, so a loose "contains" matcher here would
        // wrongly affect that earlier check too.
        _accessValidator.Setup(v => v.HasRole(_appId, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { "Clinic Admin" })))).Returns(false);

        var result = await CreateHandler().Handle(new PreviewTransferBedQuery(registration.Id, _appId, newBed.Id), CancellationToken.None);

        result.ManualChargeAllowed.Should().BeFalse();
    }
}
