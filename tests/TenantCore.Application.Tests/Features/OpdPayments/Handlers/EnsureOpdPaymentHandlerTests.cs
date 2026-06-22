using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdPayments.Commands;
using TenantCore.Application.Features.OpdPayments.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.OpdPayments.Handlers;

public class EnsureOpdPaymentHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IDoctorFeeConfigRepository> _feeRepo = new();

    [Fact]
    public async Task Handle_WhenPaymentAlreadyExists_ReturnsExistingId()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var existing = OpdPayment.Create(appId, opdId, 500);
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new EnsureOpdPaymentHandler(_paymentRepo.Object, _feeRepo.Object);
        var result = await handler.Handle(new EnsureOpdPaymentCommand(opdId, Guid.NewGuid(), appId), CancellationToken.None);

        result.Should().Be(existing.Id);
        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<OpdPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoExistingPayment_WithFeeConfig_CreatesPaymentWithFee()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var feeConfig = DoctorFeeConfig.Create(appId, doctorProfileId, 600);

        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);
        _feeRepo.Setup(r => r.GetByDoctorProfileIdAsync(doctorProfileId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeConfig);
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<OpdPayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _paymentRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new EnsureOpdPaymentHandler(_paymentRepo.Object, _feeRepo.Object);
        var result = await handler.Handle(new EnsureOpdPaymentCommand(opdId, doctorProfileId, appId), CancellationToken.None);

        result.Should().NotBeEmpty();
        _paymentRepo.Verify(r => r.AddAsync(It.Is<OpdPayment>(p => p.VisitFee == 600), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoExistingPayment_WithNoFeeConfig_CreatesPaymentWithZeroFee()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();

        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);
        _feeRepo.Setup(r => r.GetByDoctorProfileIdAsync(doctorProfileId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorFeeConfig?)null);
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<OpdPayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _paymentRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new EnsureOpdPaymentHandler(_paymentRepo.Object, _feeRepo.Object);
        var result = await handler.Handle(new EnsureOpdPaymentCommand(opdId, doctorProfileId, appId), CancellationToken.None);

        result.Should().NotBeEmpty();
        _paymentRepo.Verify(r => r.AddAsync(It.Is<OpdPayment>(p => p.VisitFee == 0), It.IsAny<CancellationToken>()), Times.Once);
    }
}
