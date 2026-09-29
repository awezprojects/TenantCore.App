using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class ChangePaymentAmountHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ChangePaymentAmountHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ChangePaymentAmountHandler CreateHandler() => new(_requestRepository.Object, _paymentRepository.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateApprovedRequest(Guid paymentId, ClinicOnboardingStatus status = ClinicOnboardingStatus.Approved)
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "CODE1", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        if (status == ClinicOnboardingStatus.AwaitingPayment)
            request.MarkAwaitingPayment();
        return request;
    }

    private static SubscriptionPayment CreatePendingPayment(Guid requestId, decimal amount = 999m) =>
        SubscriptionPayment.CreateForOnboarding(requestId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", amount, 999m, "INR", "Dr. Jane", "jane@example.test", "9876543210");

    [Fact]
    public async Task Handle_OldPaymentPendingNoLinkYet_SupersedesOldAndEnqueuesCreatePaymentLinkForReplacement()
    {
        var oldPayment = CreatePendingPayment(Guid.Empty);
        var request = CreateApprovedRequest(oldPayment.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);

        var handler = CreateHandler();
        await handler.Handle(new ChangePaymentAmountCommand(request.Id, 1500m, "Discount agreed", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        oldPayment.Status.Should().Be(SubscriptionPaymentStatus.Superseded);
        request.ApprovedAmount.Should().Be(1500m);
        request.CurrentPaymentId.Should().NotBe(oldPayment.Id);

        _paymentRepository.Verify(r => r.AddAsync(It.Is<SubscriptionPayment>(p => p.Amount == 1500m && p.ReplacesPaymentId == oldPayment.Id), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.IsAny<string>(), nameof(SubscriptionPayment), It.Is<Guid>(id => id != oldPayment.Id),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OldPaymentHasLink_EnqueuesCancelPaymentLinkAndDoesNotCreateReplacementLinkYet()
    {
        var oldPayment = CreatePendingPayment(Guid.Empty);
        oldPayment.SetLink("plink_123", "https://razorpay.test/pay/plink_123", DateTime.UtcNow.AddDays(7));
        var request = CreateApprovedRequest(oldPayment.Id, ClinicOnboardingStatus.AwaitingPayment);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);

        var handler = CreateHandler();
        await handler.Handle(new ChangePaymentAmountCommand(request.Id, 1500m, "Discount agreed", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        oldPayment.Status.Should().Be(SubscriptionPaymentStatus.LinkCreated); // not yet superseded
        oldPayment.ReplacedByPaymentId.Should().NotBeNull();
        request.CurrentPaymentId.Should().Be(oldPayment.ReplacedByPaymentId);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{oldPayment.Id}", nameof(SubscriptionPayment), oldPayment.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnchangedAmount_ThrowsInvalidOperationException()
    {
        var oldPayment = CreatePendingPayment(Guid.Empty, amount: 999m);
        var request = CreateApprovedRequest(oldPayment.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);

        var handler = CreateHandler();
        var action = () => handler.Handle(new ChangePaymentAmountCommand(request.Id, 999m, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_PaymentAlreadyPaid_ThrowsInvalidOperationException()
    {
        var oldPayment = CreatePendingPayment(Guid.Empty);
        oldPayment.SetLink("plink_123", "https://razorpay.test/pay/plink_123", DateTime.UtcNow.AddDays(7));
        oldPayment.MarkPaid("pay_123", "upi");
        var request = CreateApprovedRequest(oldPayment.Id, ClinicOnboardingStatus.AwaitingPayment);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);

        var handler = CreateHandler();
        var action = () => handler.Handle(new ChangePaymentAmountCommand(request.Id, 1500m, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WrongRequestStatus_ThrowsInvalidOperationException()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "CODE1", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null); // still Submitted — no current payment either
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.Handle(new ChangePaymentAmountCommand(request.Id, 1500m, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new ChangePaymentAmountCommand(requestId, 1500m, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PaymentRowMissing_ThrowsNotFoundException()
    {
        var paymentId = Guid.NewGuid();
        var request = CreateApprovedRequest(paymentId);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentRepository.Setup(r => r.GetByIdAsync(paymentId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new ChangePaymentAmountCommand(request.Id, 1500m, "reason", Guid.NewGuid(), "admin@example.test"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
