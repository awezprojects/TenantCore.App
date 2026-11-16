using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Authorization;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Handlers;

public class TransferBedHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly IIpdBillCalculator _billCalculator = new IpdBillCalculator(
        Mock.Of<IIpdBedTransferHistoryRepository>(), Mock.Of<IClinicFeatureFlagsRepository>());
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    private readonly Guid _appId = Guid.NewGuid();

    public TransferBedHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags());
        _ipdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ClinicFeatureFlags.Create() deliberately leaves both IPD billing settings unset (no
    // default) — tests that need a "configured" clinic must call Update() to set real values.
    private ClinicFeatureFlags ConfiguredFlags()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        return flags;
    }

    private TransferBedHandler CreateHandler() => new(
        _ipdRepository.Object, _bedRepository.Object, _historyRepository.Object,
        _featureFlagsRepository.Object, _billCalculator, _accessValidator.Object);

    private IpdRegistration BuildAdmittedRegistration(Guid currentBedId)
        => IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-20261112-0001",
            currentBedId, "Ward A", "101", "B1", 2000m, null);

    private static Bed BuildBed(Guid applicationId, bool isActive = true, bool isOccupied = false, string bedNumber = "B2")
    {
        var ward = Ward.Create(applicationId, "Ward A", null);
        var room = Room.Create(applicationId, ward.Id, "101", "General", 500m);
        var bed = Bed.Create(applicationId, ward.Id, room.Id, bedNumber);
        if (!isActive) bed.Deactivate();
        if (isOccupied) bed.MarkOccupied();
        typeof(Bed).GetProperty(nameof(Bed.Ward))!.SetValue(bed, ward);
        typeof(Bed).GetProperty(nameof(Bed.Room))!.SetValue(bed, room);
        return bed;
    }

    [Fact]
    public async Task Handle_ValidTransfer_FreesOldBedAndOccupiesNewBedInOneCall()
    {
        // DEF-IPD-01 regression: both occupancy flips must happen together.
        var oldBed = BuildBed(_appId, bedNumber: "B1");
        oldBed.MarkOccupied();
        var registration = BuildAdmittedRegistration(oldBed.Id);
        var currentSegment = IpdBedTransferHistory.Open(_appId, registration.Id, oldBed.Id, oldBed.RoomId, oldBed.WardId, 500m, Guid.NewGuid(), null);
        var newBed = BuildBed(_appId, bedNumber: "B2");

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _bedRepository.Setup(r => r.GetByIdAsync(newBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newBed);
        _bedRepository.Setup(r => r.GetByIdAsync(oldBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSegment);
        _historyRepository.Setup(r => r.AddAsync(It.IsAny<IpdBedTransferHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), "Moved to recover"), CancellationToken.None);

        oldBed.IsOccupied.Should().BeFalse();
        newBed.IsOccupied.Should().BeTrue();
        currentSegment.OccupiedTo.Should().NotBeNull();
        registration.CurrentBedId.Should().Be(newBed.Id);
    }

    [Fact]
    public async Task Handle_TargetBedIsPatientsOwnCurrentBed_ThrowsWithClearMessage()
    {
        // OBS-5: used to fall through to the generic "already occupied" check and show a
        // misleading message, since the patient's own current bed is naturally occupied.
        var currentBed = BuildBed(_appId, isOccupied: true, bedNumber: "B1");
        var registration = BuildAdmittedRegistration(currentBed.Id);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _bedRepository.Setup(r => r.GetByIdAsync(currentBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(currentBed);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, currentBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage(TenantCore.Shared.Errors.UserMessages.IpdAlreadyInThisBed);
    }

    [Fact]
    public async Task Handle_TargetBedAlreadyOccupied_ThrowsInvalidOperationException()
    {
        // Fixes DEF-IPD-02.
        var registration = BuildAdmittedRegistration(Guid.NewGuid());
        var occupiedBed = BuildBed(_appId, isOccupied: true);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _bedRepository.Setup(r => r.GetByIdAsync(occupiedBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(occupiedBed);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, occupiedBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_TargetBedDoesNotExist_ThrowsNotFoundException()
    {
        // Fixes DEF-IPD-03.
        var registration = BuildAdmittedRegistration(Guid.NewGuid());
        var missingBedId = Guid.NewGuid();

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _bedRepository.Setup(r => r.GetByIdAsync(missingBedId, It.IsAny<CancellationToken>())).ReturnsAsync((Bed?)null);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, missingBedId, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TargetBedInactive_ThrowsNotFoundException()
    {
        var registration = BuildAdmittedRegistration(Guid.NewGuid());
        var inactiveBed = BuildBed(_appId, isActive: false);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _bedRepository.Setup(r => r.GetByIdAsync(inactiveBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(inactiveBed);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, inactiveBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BillingSettingsNotConfigured_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration(Guid.NewGuid());
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RegistrationNotAdmitted_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration(Guid.NewGuid());
        registration.Discharge(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private void SetUpBasicTransfer(out IpdRegistration registration, out Bed oldBed, out Bed newBed, out IpdBedTransferHistory currentSegment)
    {
        var localOldBed = BuildBed(_appId, bedNumber: "ICU-1");
        localOldBed.MarkOccupied();
        var localRegistration = BuildAdmittedRegistration(localOldBed.Id);
        var localSegment = IpdBedTransferHistory.Open(_appId, localRegistration.Id, localOldBed.Id, localOldBed.RoomId, localOldBed.WardId, 2500m, Guid.NewGuid(), null);
        var localNewBed = BuildBed(_appId, bedNumber: "GEN-1");

        _ipdRepository.Setup(r => r.GetByIdAsync(localRegistration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(localRegistration);
        _bedRepository.Setup(r => r.GetByIdAsync(localNewBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(localNewBed);
        _bedRepository.Setup(r => r.GetByIdAsync(localOldBed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(localOldBed);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(localRegistration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localSegment);
        _historyRepository.Setup(r => r.AddAsync(It.IsAny<IpdBedTransferHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // DEF-IPD-16 test fixture: a real newBed.Room with a different (lower) rate than the
        // current ICU segment's ₹2,500, set via reflection since BuildBed's Room is normally just
        // for navigation display.
        typeof(Room).GetProperty(nameof(Room.PricePerDay))!.SetValue(localNewBed.Room, 300m);

        registration = localRegistration;
        oldBed = localOldBed;
        newBed = localNewBed;
        currentSegment = localSegment;
    }

    private void AllowManualCharge(IpdManualShiftChargeMode mode = IpdManualShiftChargeMode.AdminAndReception)
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(0, null, mode, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
    }

    [Fact]
    public async Task Handle_ManualChargeWithinCap_FreezesChargeAsManual()
    {
        AllowManualCharge();
        SetUpBasicTransfer(out var registration, out _, out var newBed, out var currentSegment);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 500m, "Clinical downgrade"),
            CancellationToken.None);

        currentSegment.StandardChargeAmount.Should().Be(2500m); // FullDayCharge mode — the segment's own full rate
        currentSegment.AppliedChargeAmount.Should().Be(500m);
        currentSegment.AdjustmentType.Should().Be(IpdBillAdjustmentType.Manual);
        currentSegment.ManualChargeReason.Should().Be("Clinical downgrade");
    }

    [Fact]
    public async Task Handle_ManualChargeAboveStandard_ThrowsDomainValidationException()
    {
        AllowManualCharge();
        SetUpBasicTransfer(out var registration, out _, out var newBed, out _);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 3000m, "Too much"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("The manual charge amount cannot exceed the standard charge of ₹2,500.00.");
    }

    [Fact]
    public async Task Handle_ManualChargeWithoutReason_FailsValidationBeforeReachingHandler()
    {
        // The validator (TransferBedCommandValidator), not the handler, owns "reason required" —
        // asserting the handler-level cap/role checks here assumes a reason was already supplied,
        // same division of labor as every other command in this codebase.
        SetUpBasicTransfer(out var registration, out _, out var newBed, out _);

        var command = new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 500m, null);
        var validator = new TenantCore.Application.Features.IpdRegistrations.Validators.TransferBedCommandValidator();

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ManualChargeWhenModeIsOff_ThrowsErrorCodeException403()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(0, null, IpdManualShiftChargeMode.Off, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out _);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 500m, "reason"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ErrorCodeException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_ManualChargeWhenAdminOnlyAndCallerIsReception_ThrowsErrorCodeException403()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(0, null, IpdManualShiftChargeMode.AdminOnly, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        // Role check is scoped to THIS clinic specifically (registration.ApplicationId) — see
        // ValidateManualCharge's SECURITY comment — never a client-supplied role list.
        // Exact single-role match — AppRoles.ReceptionRoles (checked earlier in Handle, for the
        // general "may this caller touch this registration at all" gate) also contains
        // "Clinic Admin", so a loose "contains" matcher here would wrongly short-circuit that
        // earlier check too and mask which gate actually fired.
        _accessValidator.Setup(v => v.HasRole(_appId, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { AppRoles.ClinicAdmin })))).Returns(false);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out _);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 500m, "reason"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ErrorCodeException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_ManualChargeWhenAdminOnlyAndCallerIsAdmin_Succeeds()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(0, null, IpdManualShiftChargeMode.AdminOnly, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        _accessValidator.Setup(v => v.HasRole(_appId, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { AppRoles.ClinicAdmin })))).Returns(true);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out var currentSegment);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null, 500m, "reason"),
            CancellationToken.None);

        currentSegment.AppliedChargeAmount.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_CallerLacksReceptionRoleInRegistrationsClinic_ThrowsUnauthorizedAccessException()
    {
        // SECURITY regression: CanAccess only proves the caller is LINKED to this clinic, not
        // that they hold a reception-capable role there. A user could be, say, only a Doctor in
        // this clinic but Reception in some other clinic they also belong to — the controller's
        // [Authorize(Policy=RequireReception)] resolves against the X-Application-Id header for
        // this route shape, not this registration's actual clinic, so this handler-level check is
        // the real gate.
        _accessValidator.Setup(v => v.HasRole(_appId, It.IsAny<IEnumerable<string>>())).Returns(false);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out _);

        var act = () => CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WithinGraceWindow_NewRoomOnly_FreezesOldSegmentAtZero()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(120, IpdShiftGraceRule.NewRoomOnly, IpdManualShiftChargeMode.Off, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out var currentSegment);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        currentSegment.AdjustmentType.Should().Be(IpdBillAdjustmentType.Grace);
        currentSegment.StandardChargeAmount.Should().Be(2500m); // the ICU segment's own full rate
        currentSegment.AppliedChargeAmount.Should().Be(300m);   // the new (General) room's rate only
        currentSegment.WriteOffAmount.Should().Be(2200m);
    }

    [Fact]
    public async Task Handle_WithinGraceWindow_LowerRate_ChargesTheCheaperRoom()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        flags.UpdateShiftChargingSettings(120, IpdShiftGraceRule.LowerRate, IpdManualShiftChargeMode.Off, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out var currentSegment);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        currentSegment.AppliedChargeAmount.Should().Be(300m); // min(2500, 300)
    }

    [Fact]
    public async Task Handle_OutsideGraceWindow_NoOverrideFrozen()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        // 0 minutes = off — a plain shift must behave exactly as it did before DEF-IPD-16
        // (regression requirement from the 13-Nov change request).
        flags.UpdateShiftChargingSettings(0, null, IpdManualShiftChargeMode.Off, IpdFullDayShiftRateRule.DestinationRoomRate);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(flags);
        SetUpBasicTransfer(out var registration, out _, out var newBed, out var currentSegment);

        await CreateHandler().Handle(
            new TransferBedCommand(registration.Id, _appId, newBed.Id, Guid.NewGuid(), null), CancellationToken.None);

        currentSegment.AdjustmentType.Should().BeNull();
        currentSegment.StandardChargeAmount.Should().BeNull();
        currentSegment.AppliedChargeAmount.Should().BeNull();
    }
}
