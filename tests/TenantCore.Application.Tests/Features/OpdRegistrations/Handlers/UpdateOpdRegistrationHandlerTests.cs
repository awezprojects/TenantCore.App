using FluentAssertions;
using MediatR;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.OpdRegistrations.Commands;
using TenantCore.Application.Features.OpdRegistrations.Handlers;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.OpdRegistrations.Handlers;

public class UpdateOpdRegistrationHandlerTests
{
    private readonly Mock<IOpdRegistrationRepository> _opdRepository = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Mock<ISender> _sender = new();

    private UpdateOpdRegistrationHandler CreateHandler() => new(
        _opdRepository.Object, _paymentRepository.Object, _accessValidator.Object, _sender.Object);

    private OpdRegistration CreateActiveRegistration(Guid appId)
    {
        var reg = OpdRegistration.Create(appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Test", "OPD-001", 500, null);
        _opdRepository.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reg);
        _accessValidator.Setup(v => v.CanAccess(appId)).Returns(true);
        _opdRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sender.Setup(s => s.Send(It.IsAny<ReverseUsageChargeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageChargeDto(true, 1m, 999m, false));
        return reg;
    }

    [Fact]
    public async Task Handle_CancellingActiveVisit_ReversesUsageChargeAndArmsFullRefund()
    {
        // DEF-OPD-04/05/09: cancelling for the first time reverses the usage charge exactly once
        // and owes back the full collected amount, regardless of any earlier partial-discount refund.
        var appId = Guid.NewGuid();
        var reg = CreateActiveRegistration(appId);
        var payment = OpdPayment.Create(appId, reg.Id, 500);
        payment.AcceptVisitFee(Guid.NewGuid(), null);
        payment.ApplyDiscount(50, Guid.NewGuid());
        payment.ProcessRefund(Guid.NewGuid()); // earlier partial refund already settled -> RefundStatus.Refunded

        _paymentRepository.Setup(r => r.GetByOpdRegistrationIdAsync(reg.Id, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();
        var command = new UpdateOpdRegistrationCommand(
            reg.Id, appId, reg.DoctorUserId, reg.DoctorName, reg.Fee, OpdStatus.Cancelled, null, Guid.NewGuid());
        await handler.Handle(command, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<ReverseUsageChargeCommand>(c => c.ApplicationId == appId && c.SourceEntityId == reg.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        payment.RefundStatus.Should().Be(RefundStatus.PendingRefund);
        payment.RefundDue.Should().Be(450); // the full remaining CollectedAmount, not the earlier partial amount
        _paymentRepository.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_NeverReversesUsageChargeTwice()
    {
        // The domain itself refuses to re-apply Update() to an already-cancelled registration, so a
        // repeated "cancel" PUT never reaches the reversal/refund logic a second time either way.
        var appId = Guid.NewGuid();
        var reg = CreateActiveRegistration(appId);
        reg.Update(reg.DoctorUserId, reg.DoctorName, reg.Fee, OpdStatus.Cancelled, reg.Notes);

        var handler = CreateHandler();
        var command = new UpdateOpdRegistrationCommand(
            reg.Id, appId, reg.DoctorUserId, reg.DoctorName, reg.Fee, OpdStatus.Cancelled, null, Guid.NewGuid());
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainValidationException>();
        _sender.Verify(s => s.Send(It.IsAny<ReverseUsageChargeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.GetByOpdRegistrationIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonCancellingUpdate_DoesNotTouchUsageOrPayment()
    {
        var appId = Guid.NewGuid();
        var reg = CreateActiveRegistration(appId);

        var handler = CreateHandler();
        var command = new UpdateOpdRegistrationCommand(
            reg.Id, appId, reg.DoctorUserId, reg.DoctorName, reg.Fee, OpdStatus.InProgress, null, Guid.NewGuid());
        await handler.Handle(command, CancellationToken.None);

        _sender.Verify(s => s.Send(It.IsAny<ReverseUsageChargeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.GetByOpdRegistrationIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
