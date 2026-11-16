using FluentAssertions;
using Moq;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdPayments.Services;

public class IpdBillCalculatorTests
{
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly Guid _appId = Guid.NewGuid();

    private IpdBillCalculator CreateCalculator() => new(_historyRepository.Object, _featureFlagsRepository.Object);

    private void SetupFlags(RoomBillingCycle cycle, MidStayBedChangeChargeMode mode)
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, cycle, mode);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
    }

    private static IpdBedTransferHistory BuildSegment(Guid appId, decimal rate, DateTime from, DateTime? to, string bedNumber = "B1")
    {
        var ward = Ward.Create(appId, "Ward A", null);
        var room = Room.Create(appId, ward.Id, "101", "General", rate);
        var bed = Bed.Create(appId, ward.Id, room.Id, bedNumber);
        var segment = IpdBedTransferHistory.Open(appId, Guid.NewGuid(), bed.Id, room.Id, ward.Id, rate, Guid.NewGuid(), null);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Bed))!.SetValue(segment, bed);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Room))!.SetValue(segment, room);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.Ward))!.SetValue(segment, ward);
        typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.OccupiedFrom))!.SetValue(segment, from);
        if (to.HasValue)
            typeof(IpdBedTransferHistory).GetProperty(nameof(IpdBedTransferHistory.OccupiedTo))!.SetValue(segment, to.Value);
        return segment;
    }

    private IpdRegistration BuildRegistration() =>
        IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", Guid.NewGuid(), "Ward A", "101", "B1", 0, null);

    // DEF-IPD-16 (Period-display fix): PerCalendarDay blocks now align to the machine's LOCAL
    // midnight (see IpdBillCalculator), not UTC midnight — the same convention this codebase
    // already uses for OBS-1's admission-number local date. Every PerCalendarDay test below
    // expresses "admit 10am on day 1" etc. by constructing the LOCAL wall-clock moment and
    // converting to UTC, so the test's intent (and its expected totals) stays correct no matter
    // what UTC offset the machine running it happens to have — a raw `DateTimeKind.Utc` literal
    // would silently land in the wrong calendar day whenever that offset pushes it across
    // midnight, which is exactly the bug this fix closes.
    private static DateTime Local(int year, int month, int day, int hour, int minute, int second)
        => new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local).ToUniversalTime();

    [Fact]
    public async Task CalculateRoomChargesAsync_NoSegments_ReturnsZeroWithoutRequiringSettings()
    {
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var registration = BuildRegistration();

        var result = await CreateCalculator().CalculateRoomChargesAsync(registration, DateTime.UtcNow);

        result.RoomChargesTotal.Should().Be(0);
        _featureFlagsRepository.Verify(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_SegmentsExistButSettingsNotConfigured_ThrowsInvalidOperationException()
    {
        var admit = Local(2026, 11, 1, 10, 0, 0);
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildSegment(_appId, 500m, admit, null)]);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicFeatureFlags?)null);

        var act = () => CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), admit.AddDays(2));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_PerCalendarDay_SingleRoom_ChargesOneDayPerCalendarDateTouched()
    {
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        // Admitted 10am on day 1, still admitted 2pm on day 3 — touches 3 distinct calendar dates.
        var admit = Local(2026, 11, 1, 10, 0, 0);
        var asOf = Local(2026, 11, 3, 14, 0, 0);
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildSegment(_appId, 500m, admit, null)]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        result.RoomChargesTotal.Should().Be(1500m); // 3 calendar days x 500
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_Per24HourBlock_SingleRoom_ChargesByElapsedBlocksFromAdmission()
    {
        SetupFlags(RoomBillingCycle.Per24HourBlock, MidStayBedChangeChargeMode.FullDayCharge);
        var admit = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        var asOf = admit.AddHours(50); // 2 full 24h blocks + a fraction of a 3rd
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildSegment(_appId, 500m, admit, null)]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        result.RoomChargesTotal.Should().Be(1500m); // 3 blocks entered (ceil(50/24)) x 500
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_PerCalendarDay_FullDayChargeOnTransferDay_ChargesDestinationRoomOnce()
    {
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        var admit = Local(2026, 11, 1, 10, 0, 0);
        var transfer = Local(2026, 11, 2, 14, 0, 0); // mid-day on day 2
        var asOf = Local(2026, 11, 2, 23, 0, 0); // still day 2
        var oldSegment = BuildSegment(_appId, 500m, admit, transfer, "B1");
        var newSegment = BuildSegment(_appId, 800m, transfer, null, "B2");
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldSegment, newSegment]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        // Day 1 (old room, 500) + Day 2, which has a transfer — FullDayCharge bills the
        // destination room (800) once, not both and not split.
        result.RoomChargesTotal.Should().Be(500m + 800m);
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_PerCalendarDay_Prorated_SplitsTransferDayByTimeShare()
    {
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.Prorated);
        var admit = Local(2026, 11, 1, 0, 0, 0);
        // Transfer exactly at midday on day 1 itself — half the day in each room.
        var transfer = Local(2026, 11, 1, 12, 0, 0);
        var asOf = Local(2026, 11, 2, 0, 0, 0);
        var oldSegment = BuildSegment(_appId, 400m, admit, transfer, "B1");
        var newSegment = BuildSegment(_appId, 800m, transfer, null, "B2");
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldSegment, newSegment]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        // Half of 400 (old room) + half of 800 (new room) = 200 + 400 = 600.
        result.RoomChargesTotal.Should().Be(600m);
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_PerCalendarDay_Prorated_RapidSameDayShiftAndDischarge_NeverRoundsToZero()
    {
        // DEF-IPD-16 prorated-same-day bug: a rapid admit→shift→discharge (each segment lasting
        // only seconds) used to round EVERY segment's tiny per-day share down to ₹0.00
        // individually, silently billing nothing for a day the patient really did occupy a room.
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.Prorated);
        var admit = Local(2026, 11, 1, 10, 0, 0);
        var transfer = admit.AddSeconds(1);
        var asOf = admit.AddSeconds(2);
        var oldSegment = BuildSegment(_appId, 300m, admit, transfer, "B1");
        var newSegment = BuildSegment(_appId, 2500m, transfer, null, "B2");
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldSegment, newSegment]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        result.RoomChargesTotal.Should().BeGreaterThan(0m);
        result.RoomChargesTotal.Should().BeLessThanOrEqualTo(2500m);
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_FullDayChargeOnTransferDay_OriginRoomRateRule_ChargesOriginRoom()
    {
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(0, null, IpdManualShiftChargeMode.Off, IpdFullDayShiftRateRule.OriginRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);

        var admit = Local(2026, 11, 1, 0, 0, 0);
        var transfer = Local(2026, 11, 1, 12, 0, 0);
        var asOf = Local(2026, 11, 2, 0, 0, 0);
        var oldSegment = BuildSegment(_appId, 2500m, admit, transfer, "B1");
        var newSegment = BuildSegment(_appId, 300m, transfer, null, "B2");
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldSegment, newSegment]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        result.RoomChargesTotal.Should().Be(2500m); // origin (ICU) rate, not destination
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_FrozenGraceOverride_SettlesWholeBlockAndReportsWriteOff()
    {
        // DEF-IPD-16: this is the calculator-side half of TransferBedHandler's grace logic —
        // verifies a segment closed with a frozen override is read back correctly into the bill,
        // not just that the handler wrote the right fields (see TransferBedHandlerTests).
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        var admit = Local(2026, 11, 1, 10, 0, 0);
        var transfer = admit.AddHours(1);
        var asOf = admit.AddHours(2);
        var oldSegment = BuildSegment(_appId, 2500m, admit, transfer, "B1");
        oldSegment.FreezeCharge(2500m, 300m, IpdBillAdjustmentType.Grace);
        var newSegment = BuildSegment(_appId, 300m, transfer, null, "B2");
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldSegment, newSegment]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), asOf);

        result.RoomChargesTotal.Should().Be(300m);
        result.WriteOffTotal.Should().Be(2200m);
        result.LineItems.Should().ContainSingle(li => li.AdjustmentType == IpdBillAdjustmentType.Grace);
    }

    [Fact]
    public async Task CalculateRoomChargesAsync_AsOfBeforeAdmission_ReturnsZero()
    {
        SetupFlags(RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        var admit = Local(2026, 11, 5, 10, 0, 0);
        _historyRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildSegment(_appId, 500m, admit, null)]);

        var result = await CreateCalculator().CalculateRoomChargesAsync(BuildRegistration(), admit.AddHours(-1));

        result.RoomChargesTotal.Should().Be(0);
    }
}
