using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class CancelClinicPaymentLinkHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public CancelClinicPaymentLinkHandlerTests()
    {
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) =>
                WorkflowTask.Enqueue(t, k, at, aid, p));
    }

    private CancelClinicPaymentLinkHandler CreateHandler()
        => new(_paymentRepository.Object, _workflowEnqueuer.Object);

    private CancelClinicPaymentLinkCommand Command(Guid paymentId)
        => new(_applicationId, paymentId, Guid.NewGuid(), "admin@example.test");

    private SubscriptionPayment OpenRenewal()
    {
        var payment = SubscriptionPayment.CreateForRenewal(
            _applicationId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, "INR",
            "Dr Mehta", "doctor@example.test", null, Guid.NewGuid());
        payment.SetLink("plink_abc", "https://rzp.io/x", DateTime.UtcNow.AddDays(7));
        return payment;
    }

    [Fact]
    public async Task Handle_OpenLink_EnqueuesCancelWithNoReplacement()
    {
        var payment = OpenRenewal();
        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(payment.Id, _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        await CreateHandler().Handle(Command(payment.Id), CancellationToken.None);

        payment.ReplacedByPaymentId.Should().BeNull("a plain cancel has no replacement to create afterwards");
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{payment.Id}",
            nameof(SubscriptionPayment), payment.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyPaid_ThrowsInvalidOperationException()
    {
        var payment = OpenRenewal();
        payment.MarkPaid("pay_123", "upi");
        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(payment.Id, _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var act = async () => await CreateHandler().Handle(Command(payment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_OnboardingPayment_ThrowsInvalidOperationException()
    {
        var payment = SubscriptionPayment.CreateForOnboarding(
            Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR",
            "Dr Mehta", "doctor@example.test", null);
        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(payment.Id, _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var act = async () => await CreateHandler().Handle(Command(payment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*renewal or admin-assigned*");
    }

    [Fact]
    public async Task Handle_PaymentOfAnotherClinic_ThrowsNotFoundException()
    {
        _paymentRepository.Setup(r => r.GetByIdForClinicAsync(It.IsAny<Guid>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
