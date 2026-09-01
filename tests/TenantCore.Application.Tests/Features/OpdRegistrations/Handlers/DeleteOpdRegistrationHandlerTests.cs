using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdRegistrations.Handlers;

public class DeleteOpdRegistrationHandlerTests
{
    private readonly Mock<IOpdRegistrationRepository> _opdRepository = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOpdParticularRepository> _particularRepository = new();

    private DeleteOpdRegistrationHandler CreateHandler() =>
        new(_opdRepository.Object, _paymentRepository.Object, _particularRepository.Object);

    private static OpdRegistration CreateCancelledRegistration(Guid appId)
    {
        var reg = OpdRegistration.Create(appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Test", "OPD-001", 500, null);
        reg.Update(reg.DoctorUserId, reg.DoctorName, reg.Fee, OpdStatus.Cancelled, reg.Notes);
        return reg;
    }

    [Fact]
    public async Task Handle_CancelledWithNoPayment_DeletesRegistrationAndSavesChanges()
    {
        var appId = Guid.NewGuid();
        var registration = CreateCancelledRegistration(appId);

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);
        _particularRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular>());
        _opdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, appId), CancellationToken.None);

        _opdRepository.Verify(r => r.Delete(registration), Times.Once);
        _opdRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CancelledWithRefundedPayment_DeletesPaymentAndRegistration()
    {
        var appId = Guid.NewGuid();
        var registration = CreateCancelledRegistration(appId);
        var payment = OpdPayment.Create(appId, registration.Id, 500);
        payment.AcceptVisitFee(Guid.NewGuid(), null);
        payment.ApplyDiscount(500); // fully discounted after collection -> refund due
        payment.ProcessRefund(Guid.NewGuid());

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _particularRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular>());
        _opdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, appId), CancellationToken.None);

        _paymentRepository.Verify(r => r.Delete(payment), Times.Once);
        _opdRepository.Verify(r => r.Delete(registration), Times.Once);
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _opdRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdRegistration?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new DeleteOpdRegistrationCommand(id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_EntityBelongingToDifferentTenant_ThrowsNotFoundException()
    {
        var commandApplicationId = Guid.NewGuid();
        var entityApplicationId = Guid.NewGuid();
        var registration = CreateCancelledRegistration(entityApplicationId);

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var handler = CreateHandler();
        var action = () => handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, commandApplicationId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NotCancelled_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var registration = OpdRegistration.Create(appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Test", "OPD-002", 500, null);

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        var handler = CreateHandler();
        var action = () => handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CollectedPaymentNotRefunded_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var registration = CreateCancelledRegistration(appId);
        var payment = OpdPayment.Create(appId, registration.Id, 500);
        payment.AcceptVisitFee(Guid.NewGuid(), null);

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();
        var action = () => handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_IndividuallyCollectedParticularNotRefunded_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var registration = CreateCancelledRegistration(appId);
        var particular = OpdParticular.Create(appId, registration.Id, Guid.NewGuid(), "Consultation", 100);
        particular.Collect(Guid.NewGuid(), Guid.NewGuid());

        _opdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);
        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);
        _particularRepository.Setup(r => r.GetByOpdRegistrationIdAsync(registration.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular> { particular });

        var handler = CreateHandler();
        var action = () => handler.Handle(new DeleteOpdRegistrationCommand(registration.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _opdRepository.Verify(r => r.Delete(It.IsAny<OpdRegistration>()), Times.Never);
    }
}
