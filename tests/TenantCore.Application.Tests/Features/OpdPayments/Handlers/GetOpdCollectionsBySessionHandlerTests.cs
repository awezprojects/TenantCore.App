using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdPayments.Handlers;
using TenantCore.Application.Features.OpdPayments.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdPayments.Handlers;

public class GetOpdCollectionsBySessionHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOpdParticularRepository> _particularRepository = new();
    private readonly Mock<IOpdRegistrationRepository> _opdRepository = new();

    private GetOpdCollectionsBySessionHandler CreateHandler() =>
        new(_paymentRepository.Object, _particularRepository.Object, _opdRepository.Object);

    [Fact]
    public async Task Handle_NoActivity_ReturnsEmptyList()
    {
        var appId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetOpdCollectionsBySessionQuery(sessionId, appId), CancellationToken.None);

        result.Should().BeEmpty();
        _opdRepository.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), appId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaymentCollectedThisSession_ReturnsRowWithTotals()
    {
        var appId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var patient = Patient.Create(appId, "Jane", "Doe", null, Gender.Female, "9999999999", null, null, null, null);
        var registration = OpdRegistration.Create(appId, patient.Id, Guid.NewGuid(), "Dr. Smith", "OPD-001", 300, null);
        typeof(OpdRegistration).GetProperty("Patient")!.SetValue(registration, patient);

        var payment = OpdPayment.Create(appId, registration.Id, 300);
        payment.AcceptVisitFee(Guid.NewGuid(), sessionId);

        _paymentRepository.Setup(r => r.GetBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular>());
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _opdRepository.Setup(r => r.GetByIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdRegistration> { registration });

        var handler = CreateHandler();
        var result = (await handler.Handle(new GetOpdCollectionsBySessionQuery(sessionId, appId), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].OpdRegistrationId.Should().Be(registration.Id);
        result[0].PatientName.Should().Be("Jane Doe");
        result[0].ConsultationFee.Should().Be(300);
        result[0].ItemsTotal.Should().Be(0);
        result[0].TotalCollected.Should().Be(300);
        result[0].HasItems.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IndividuallyCollectedItemsOnly_IncludesItemsTotalInResult()
    {
        var appId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var patient = Patient.Create(appId, "John", "Smith", null, Gender.Male, "8888888888", null, null, null, null);
        var registration = OpdRegistration.Create(appId, patient.Id, Guid.NewGuid(), "Dr. Lee", "OPD-002", 200, null);
        typeof(OpdRegistration).GetProperty("Patient")!.SetValue(registration, patient);

        var payment = OpdPayment.Create(appId, registration.Id, 200);
        payment.UpdateParticularsTotal(100);

        var particular = OpdParticular.Create(appId, registration.Id, Guid.NewGuid(), "Dressing", 100);
        particular.Collect(Guid.NewGuid(), sessionId);

        _paymentRepository.Setup(r => r.GetBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _particularRepository.Setup(r => r.GetCollectedBySessionIdAsync(sessionId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular> { particular });
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _opdRepository.Setup(r => r.GetByIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdRegistration> { registration });

        var handler = CreateHandler();
        var result = (await handler.Handle(new GetOpdCollectionsBySessionQuery(sessionId, appId), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].ConsultationFee.Should().Be(200);
        result[0].ItemsTotal.Should().Be(100);
        result[0].TotalCollected.Should().Be(100);
        result[0].HasItems.Should().BeTrue();
    }
}
