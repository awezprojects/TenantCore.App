using FluentAssertions;
using MediatR;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Handlers;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Handlers;

public class CancelIpdRegistrationHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Mock<ISender> _sender = new();

    private readonly Guid _appId = Guid.NewGuid();

    public CancelIpdRegistrationHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
        _ipdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        // Billing disabled for these tests unless a specific test overrides it.
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(It.IsAny<Guid>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdPayment?)null);
        // No ClinicFeatureFlags row (null) ⇒ IpdCancelWindowMinutes defaults to 0 (unlimited) in
        // the handler — matches every pre-existing test's expectation that Cancel is always free.
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);
        _sender.Setup(s => s.Send(It.IsAny<ReverseUsageChargeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageChargeDto(false, 0m, null, true));
    }

    private CancelIpdRegistrationHandler CreateHandler() => new(
        _ipdRepository.Object, _bedRepository.Object, _historyRepository.Object, _paymentRepository.Object,
        _featureFlagsRepository.Object, _accessValidator.Object, _sender.Object);

    private IpdRegistration BuildAdmittedRegistration(Guid? currentBedId)
        => IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-20261112-0001",
            currentBedId, "Ward A", "101", "B1", 2000m, null);

    private static void BackdateAdmission(IpdRegistration registration, DateTime admissionDateUtc)
        => typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.AdmissionDate))!.SetValue(registration, admissionDateUtc);

    [Fact]
    public async Task Handle_AdmittedWithBed_FreesBedAndSetsCancelled()
    {
        var bed = Bed.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        bed.MarkOccupied();
        var registration = BuildAdmittedRegistration(bed.Id);
        var segment = IpdBedTransferHistory.Open(_appId, registration.Id, bed.Id, bed.RoomId, bed.WardId, 500m, Guid.NewGuid(), null);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(segment);
        _bedRepository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);

        var result = await CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, "Wrong patient admitted"), CancellationToken.None);

        bed.IsOccupied.Should().BeFalse();
        result.Status.Should().Be(IpdStatus.Cancelled);
        _ipdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // DEF-IPD-19: cancelling always reverses the admission usage charge, even when billing
        // itself is disabled for the clinic (no IpdPayment row) — the two are independent.
        _sender.Verify(s => s.Send(
            It.Is<ReverseUsageChargeCommand>(c => c.BillingItem == BillingItem.IpdAdmission && c.SourceEntityId == registration.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDeposits_FinalizesBillAtZeroAndRefundsFullDeposit()
    {
        var registration = BuildAdmittedRegistration(null);
        var payment = IpdPayment.Create(_appId, registration.Id, 500m);
        payment.IncreaseDeposited(700m);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        await CreateHandler().Handle(new CancelIpdRegistrationCommand(registration.Id, _appId, "Wrong patient admitted"), CancellationToken.None);

        // No room charge, no initial fee — the whole ₹500 initial fee is waived, not just the room.
        payment.TotalAmount.Should().Be(0m);
        payment.FinalAmount.Should().Be(0m);
        payment.RoomChargesTotal.Should().Be(0m);
        payment.RefundDue.Should().Be(700m);
        payment.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        payment.FinalizedAt.Should().NotBeNull();
        _paymentRepository.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_WithServiceItemsAdded_ThrowsDomainValidationException()
    {
        var registration = BuildAdmittedRegistration(null);
        var payment = IpdPayment.Create(_appId, registration.Id, 0m);
        payment.IncreaseParticularsTotal(150m);

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var act = () => CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("Remove the service items or discharge the patient instead.");
        _ipdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsNotFoundException()
    {
        _ipdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((IpdRegistration?)null);

        var act = () => CreateHandler().Handle(new CancelIpdRegistrationCommand(Guid.NewGuid(), _appId, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDischarged_ThrowsDomainValidationException()
    {
        var registration = BuildAdmittedRegistration(null);
        registration.Discharge(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);

        var act = () => CreateHandler().Handle(new CancelIpdRegistrationCommand(registration.Id, _appId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Handle_CallerLacksReceptionRoleInRegistrationsClinic_ThrowsUnauthorizedAccessException()
    {
        // SECURITY: CanAccess alone only proves clinic linkage, not a reception-capable role in
        // THIS specific clinic — e.g. a Doctor-only user here who also holds Reception somewhere
        // else must still be refused. This action refunds deposits and reverses a usage charge,
        // so it must not be gateable via a role held in a different clinic.
        var registration = BuildAdmittedRegistration(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _accessValidator.Setup(v => v.HasRole(_appId, It.IsAny<IEnumerable<string>>())).Returns(false);

        var act = () => CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, "Wrong patient"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _ipdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private ClinicFeatureFlags BuildFlagsWithCancelWindow(int minutes)
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.UpdateCancelWindow(minutes);
        return flags;
    }

    [Fact]
    public async Task Handle_WithinCancelWindow_Succeeds()
    {
        // DEF-IPD-26 (15-Nov): admitted 30 minutes ago, 60-minute window — still inside it.
        var registration = BuildAdmittedRegistration(null);
        BackdateAdmission(registration, DateTime.UtcNow.AddMinutes(-30));
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFlagsWithCancelWindow(60));

        var result = await CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, "Wrong patient"), CancellationToken.None);

        result.Status.Should().Be(IpdStatus.Cancelled);
        _ipdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PastCancelWindow_ThrowsDomainValidationExceptionAndDoesNotSave()
    {
        // DEF-IPD-26 (15-Nov): admitted 90 minutes ago, 60-minute window — past it. Discharge is
        // the only remaining path; cancel must be refused outright, not silently allowed.
        var registration = BuildAdmittedRegistration(null);
        BackdateAdmission(registration, DateTime.UtcNow.AddMinutes(-90));
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFlagsWithCancelWindow(60));

        var act = () => CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, "Changed my mind"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*more than 60 minute(s) ago*Discharge the patient instead*");
        registration.Status.Should().Be(IpdStatus.Admitted, "a refused cancel must leave the admission untouched");
        _ipdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _historyRepository.Verify(r => r.GetCurrentSegmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "the bed/segment must not be touched when the cancel itself is refused");
    }

    [Fact]
    public async Task Handle_CancelWindowUnlimited_AllowsCancelRegardlessOfElapsedTime()
    {
        // 0 = unlimited — explicit regression for "no existing clinic's cancel policy changes."
        var registration = BuildAdmittedRegistration(null);
        BackdateAdmission(registration, DateTime.UtcNow.AddDays(-30));
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildFlagsWithCancelWindow(0));

        var result = await CreateHandler().Handle(
            new CancelIpdRegistrationCommand(registration.Id, _appId, null), CancellationToken.None);

        result.Status.Should().Be(IpdStatus.Cancelled);
    }
}
