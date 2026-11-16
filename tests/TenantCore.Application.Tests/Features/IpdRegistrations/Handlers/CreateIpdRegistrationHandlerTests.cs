using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.IpdRegistrations.Commands;
using TenantCore.Application.Features.IpdRegistrations.Handlers;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdRegistrations.Handlers;

public class CreateIpdRegistrationHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<ILogger<CreateIpdRegistrationHandler>> _logger = new();

    private readonly Guid _appId = Guid.NewGuid();
    private readonly Patient _patient;

    public CreateIpdRegistrationHandlerTests()
    {
        _patient = Patient.Create(_appId, "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
    }

    private CreateIpdRegistrationHandler CreateHandler() => new(
        _ipdRepository.Object, _patientRepository.Object, _bedRepository.Object,
        _historyRepository.Object, _featureFlagsRepository.Object, _paymentRepository.Object,
        _sender.Object, _logger.Object);

    private void SetupCommonMocks()
    {
        _patientRepository.Setup(r => r.GetByIdAsync(_patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_patient);
        _ipdRepository.Setup(r => r.HasActiveAdmissionAsync(_patient.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ipdRepository.Setup(r => r.GetNextAdmissionNumberAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("IPD-20261112-0001");
        _ipdRepository.Setup(r => r.AddAsync(It.IsAny<IpdRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ipdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<IpdPayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(s => s.Send(It.IsAny<ChargeUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageChargeDto(true, 10m, null, false));
    }

    // ClinicFeatureFlags.Create() deliberately leaves both IPD billing settings unset (no
    // default) — tests that need a "configured" clinic must call Update() to set real values.
    private ClinicFeatureFlags ConfiguredFlags()
    {
        var flags = ClinicFeatureFlags.Create(_appId);
        flags.Update(true, true, RoomBillingCycle.PerCalendarDay, MidStayBedChangeChargeMode.FullDayCharge);
        return flags;
    }

    // The handler reloads the entity via GetByIdAsync after SaveChanges (to get Patient
    // populated for the translator) — mock that round trip instead of returning null.
    private void StubReloadAfterSave()
    {
        IpdRegistration? added = null;
        _ipdRepository.Setup(r => r.AddAsync(It.IsAny<IpdRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<IpdRegistration, CancellationToken>((entity, _) =>
            {
                typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(entity, _patient);
                added = entity;
            })
            .Returns(Task.CompletedTask);
        _ipdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);
    }

    private Bed BuildOccupiableBed(Guid wardId, Guid roomId, bool isActive = true, bool isOccupied = false)
    {
        var ward = Ward.Create(_appId, "General Ward", null);
        var room = Room.Create(_appId, ward.Id, "101", "General", 500m);
        var bed = Bed.Create(_appId, ward.Id, room.Id, "B1");
        if (!isActive) bed.Deactivate();
        if (isOccupied) bed.MarkOccupied();

        typeof(Bed).GetProperty(nameof(Bed.Ward))!.SetValue(bed, ward);
        typeof(Bed).GetProperty(nameof(Bed.Room))!.SetValue(bed, room);
        return bed;
    }

    [Fact]
    public async Task Handle_WardOnlyAdmission_SucceedsWithoutBillingSettingsConfigured()
    {
        SetupCommonMocks();
        // Flags left unstubbed (null) — a ward-only admission (no BedId) must never require
        // IpdRoomBillingCycle/IpdMidStayChargeMode to be configured, even though the handler
        // still reads flags once for the unrelated BillingEnabled check.
        StubReloadAfterSave();

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", null, 2000m, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _bedRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BedAllocationWithoutBillingSettingsConfigured_ThrowsInvalidOperationException()
    {
        SetupCommonMocks();
        var bed = BuildOccupiableBed(Guid.NewGuid(), Guid.NewGuid());
        _bedRepository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", bed.Id, 2000m, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_BedAllocationWithBillingSettingsConfigured_OccupiesBedAndOpensHistorySegment()
    {
        SetupCommonMocks();
        var bed = BuildOccupiableBed(Guid.NewGuid(), Guid.NewGuid());
        _bedRepository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags());
        _historyRepository.Setup(r => r.AddAsync(It.IsAny<IpdBedTransferHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        StubReloadAfterSave();

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", bed.Id, 2000m, null);
        await CreateHandler().Handle(command, CancellationToken.None);

        bed.IsOccupied.Should().BeTrue();
        _historyRepository.Verify(r => r.AddAsync(
            It.Is<IpdBedTransferHistory>(h => h.BedId == bed.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BedAlreadyOccupied_ThrowsInvalidOperationException()
    {
        SetupCommonMocks();
        var bed = BuildOccupiableBed(Guid.NewGuid(), Guid.NewGuid(), isOccupied: true);
        _bedRepository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags());

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", bed.Id, 2000m, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_BedNotFound_ThrowsNotFoundException()
    {
        SetupCommonMocks();
        var missingBedId = Guid.NewGuid();
        _bedRepository.Setup(r => r.GetByIdAsync(missingBedId, It.IsAny<CancellationToken>())).ReturnsAsync((Bed?)null);
        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(_appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfiguredFlags());

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", missingBedId, 2000m, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PatientAlreadyAdmitted_ThrowsInvalidOperationException()
    {
        SetupCommonMocks();
        _ipdRepository.Setup(r => r.HasActiveAdmissionAsync(_patient.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateIpdRegistrationCommand(_appId, _patient.Id, Guid.NewGuid(), "Dr. Smith", null, 2000m, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
