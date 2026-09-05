using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdRegistrations.Handlers;

public class CreateOpdRegistrationHandlerTests
{
    private readonly Mock<IOpdRegistrationRepository> _opdRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IClinicFeeConfigRepository> _feeRepository = new();
    private readonly Mock<IClinicFeatureFlagsRepository> _featureFlagsRepository = new();
    private readonly Mock<IDoctorProfileRepository> _doctorProfileRepository = new();
    private readonly Mock<ICounterSessionRepository> _counterSessionRepository = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<ILogger<CreateOpdRegistrationHandler>> _logger = new();

    private CreateOpdRegistrationHandler CreateHandler() => new(
        _opdRepository.Object, _patientRepository.Object, _feeRepository.Object,
        _featureFlagsRepository.Object, _doctorProfileRepository.Object,
        _counterSessionRepository.Object, _paymentRepository.Object,
        _sender.Object, _logger.Object);

    private void SetupCommonMocks(Guid appId, CounterSession activeSession, Patient patient)
    {
        _counterSessionRepository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSession);
        _patientRepository.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _opdRepository.Setup(r => r.GetNextRegistrationNumberAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("OPD-20260101-0001");
        _opdRepository.Setup(r => r.AddAsync(It.IsAny<OpdRegistration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _opdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _doctorProfileRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorProfile?)null);
        _sender.Setup(s => s.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
    }

    private static Patient CreatePatient(Guid appId) =>
        Patient.Create(appId, "Jane", "Doe", null, Gender.Female, "9999999999", null, null, null, null);

    [Fact]
    public async Task Handle_PrepaidEnabled_AutoAcceptsVisitFee()
    {
        var appId = Guid.NewGuid();
        var patient = CreatePatient(appId);
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        var receivedBy = Guid.NewGuid();
        SetupCommonMocks(appId, session, patient);

        OpdRegistration? added = null;
        _opdRepository.Setup(r => r.AddAsync(It.IsAny<OpdRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<OpdRegistration, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicFeatureFlags.Create(appId, true));

        OpdPayment? payment = null;
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(It.IsAny<Guid>(), appId, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(payment ??= OpdPayment.Create(appId, added!.Id, 500)));
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _opdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(added));

        var handler = CreateHandler();
        var command = new CreateOpdRegistrationCommand(
            appId, patient.Id, Guid.NewGuid(), "Dr. Smith", 500, null, null, null, null, null, null, null, null, receivedBy);
        await handler.Handle(command, CancellationToken.None);

        payment.Should().NotBeNull();
        payment!.PaymentStatus.Should().Be(PaymentStatus.Received);
        payment.ReceivedByUserId.Should().Be(receivedBy);
        _paymentRepository.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_PrepaidDisabled_LeavesPaymentPending()
    {
        var appId = Guid.NewGuid();
        var patient = CreatePatient(appId);
        var session = CounterSession.Create(appId, Guid.NewGuid(), DateTime.Today);
        SetupCommonMocks(appId, session, patient);

        OpdRegistration? added = null;
        _opdRepository.Setup(r => r.AddAsync(It.IsAny<OpdRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<OpdRegistration, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        _featureFlagsRepository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClinicFeatureFlags.Create(appId, false));

        OpdPayment? payment = null;
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(It.IsAny<Guid>(), appId, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(payment ??= OpdPayment.Create(appId, added!.Id, 500)));

        _opdRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(added));

        var handler = CreateHandler();
        var command = new CreateOpdRegistrationCommand(
            appId, patient.Id, Guid.NewGuid(), "Dr. Smith", 500, null, null, null, null, null, null, null, null, Guid.NewGuid());
        await handler.Handle(command, CancellationToken.None);

        // Prepaid disabled — handler must not touch the payment at all; it stays Pending.
        _paymentRepository.Verify(r => r.GetByOpdRegistrationIdAsync(It.IsAny<Guid>(), appId, It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.Update(It.IsAny<OpdPayment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoActiveCounterSession_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        _counterSessionRepository.Setup(r => r.GetActiveSessionAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterSession?)null);

        var handler = CreateHandler();
        var command = new CreateOpdRegistrationCommand(
            appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", 500, null, null, null, null, null, null, null, null, Guid.NewGuid());

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
