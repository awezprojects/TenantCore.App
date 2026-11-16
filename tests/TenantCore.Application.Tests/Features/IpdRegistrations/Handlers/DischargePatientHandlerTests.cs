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

public class DischargePatientHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IBedRepository> _bedRepository = new();
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Mock<ISender> _sender = new();

    private readonly Guid _appId = Guid.NewGuid();

    public DischargePatientHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _sender.Setup(s => s.Send(It.IsAny<ChargeUsageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageChargeDto(true, 100m, null, false));
    }

    private DischargePatientHandler CreateHandler() => new(
        _ipdRepository.Object, _bedRepository.Object, _historyRepository.Object, _accessValidator.Object, _sender.Object);

    private IpdRegistration BuildAdmittedRegistration(Guid? currentBedId)
        => IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-20261112-0001",
            currentBedId, "Ward A", "101", "B2", 2000m, null);

    [Fact]
    public async Task Handle_PatientWithOpenTransferSegment_FreesTheBedCurrentlyPointedToByTheSegment()
    {
        // Regression test for DEF-IPD-01: after a bed shift, the bed that must be freed on
        // discharge is the one in the OPEN history segment (B2), not whatever name string an
        // earlier, buggy flow might have left behind.
        var registration = BuildAdmittedRegistration(currentBedId: Guid.NewGuid());
        var segment = IpdBedTransferHistory.Open(_appId, registration.Id, registration.CurrentBedId!.Value,
            Guid.NewGuid(), Guid.NewGuid(), 500m, Guid.NewGuid(), null);
        var bed = Bed.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "B2");
        bed.MarkOccupied();

        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(segment);
        _bedRepository.Setup(r => r.GetByIdAsync(segment.BedId, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _ipdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateHandler().Handle(new DischargePatientCommand(registration.Id, _appId, "Recovered"), CancellationToken.None);

        bed.IsOccupied.Should().BeFalse();
        segment.OccupiedTo.Should().NotBeNull();
        _historyRepository.Verify(r => r.Update(segment), Times.Once);
    }

    [Fact]
    public async Task Handle_NoOpenTransferSegment_DischargesWithoutTouchingAnyBed()
    {
        var registration = BuildAdmittedRegistration(currentBedId: null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _historyRepository.Setup(r => r.GetCurrentSegmentAsync(registration.Id, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpdBedTransferHistory?)null);
        _ipdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new DischargePatientCommand(registration.Id, _appId, null), CancellationToken.None);

        result.Status.Should().Be(IpdStatus.Discharged);
        _bedRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsNotFoundException()
    {
        _ipdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((IpdRegistration?)null);

        var act = () => CreateHandler().Handle(new DischargePatientCommand(Guid.NewGuid(), _appId, null), CancellationToken.None);

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

        var act = () => CreateHandler().Handle(new DischargePatientCommand(registration.Id, _appId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
