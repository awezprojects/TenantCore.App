using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

public class CancelPaymentLinkTaskHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public CancelPaymentLinkTaskHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CancelPaymentLinkTaskHandler CreateHandler() =>
        new(_paymentRepository.Object, _requestRepository.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

    private static SubscriptionPayment CreateLinkedPayment(Guid requestId, decimal amount = 999m)
    {
        var payment = SubscriptionPayment.CreateForOnboarding(requestId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", amount, amount, "INR", "Dr", "dr@example.test", "9876543210");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        return payment;
    }

    private static WorkflowTask CreateTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.CancelPaymentLink, $"cancel-link:{paymentId}", nameof(SubscriptionPayment), paymentId);

    // ---- Rejection case (no ReplacedByPaymentId) ----

    [Fact]
    public async Task HandleAsync_RejectionCase_SuccessfulCancel_MarksPaymentCancelled()
    {
        var payment = CreateLinkedPayment(Guid.NewGuid());
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Ok());

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Cancelled);
    }

    [Fact]
    public async Task HandleAsync_RejectionCase_NoLinkWasEverCreated_MarksCancelledWithoutCallingGateway()
    {
        var payment = SubscriptionPayment.CreateForOnboarding(Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr", "dr@example.test", "9876543210");
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Cancelled);
        _paymentGateway.Verify(g => g.CancelPaymentLinkAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ClinicLinkAlreadyPaidWithNoReplacement_ConfirmsItInsteadOfDroppingTheMoney()
    {
        // A renewal or admin link paid at the instant it was being cancelled has no onboarding
        // request to flag and no replacement to fall back on. It must still be confirmed, or the
        // clinic would have paid for a term that never activates.
        var applicationId = Guid.NewGuid();
        var payment = SubscriptionPayment.CreateForRenewal(
            applicationId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, "INR",
            "Admin", "admin@clinic.test", null, Guid.NewGuid());
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GatewayResult.Permanent("already paid"));
        var paidLink = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayResult.Ok(), paidLink));

        await CreateHandler().HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}",
            nameof(SubscriptionPayment), payment.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);

        payment.Status.Should().NotBe(SubscriptionPaymentStatus.Cancelled, "the payment actually happened");
    }

    [Fact]
    public async Task HandleAsync_RejectionCase_LinkAlreadyPaid_FlagsRefundRequiredAndNeverAutoProvisions()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        var payment = CreateLinkedPayment(request.Id);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(payment.Id);
        request.MarkAwaitingPayment();
        request.Reject("closed", Guid.NewGuid(), "admin@example.test");

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Permanent("already paid"));
        var paidLink = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), paidLink));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        request.NeedsAttention.Should().BeTrue();
        request.AttentionReason.Should().Contain("refund");
        payment.Status.Should().NotBe(SubscriptionPaymentStatus.Cancelled);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Change-amount case (ReplacedByPaymentId set) ----

    [Fact]
    public async Task HandleAsync_ChangeAmountCase_SuccessfulCancel_SupersedesOldAndEnqueuesReplacementLink()
    {
        var oldPayment = CreateLinkedPayment(Guid.NewGuid());
        var replacement = SubscriptionPayment.CreateForOnboarding(Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 1500m, 999m, "INR", "Dr", "dr@example.test", "9876543210", oldPayment.Id);
        oldPayment.SetReplacement(replacement.Id);

        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Ok());

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(oldPayment.Id), CancellationToken.None);

        oldPayment.Status.Should().Be(SubscriptionPaymentStatus.Superseded);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, $"create-link:{replacement.Id}", nameof(SubscriptionPayment), replacement.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChangeAmountCase_OldLinkAlreadyPaid_KeepsOldConfirmsItAndCancelsReplacementWithoutCreatingItsLink()
    {
        var oldPayment = CreateLinkedPayment(Guid.NewGuid());
        var replacement = SubscriptionPayment.CreateForOnboarding(Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 1500m, 999m, "INR", "Dr", "dr@example.test", "9876543210", oldPayment.Id);
        oldPayment.SetReplacement(replacement.Id);

        _paymentRepository.Setup(r => r.GetByIdAsync(oldPayment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oldPayment);
        _paymentRepository.Setup(r => r.GetByIdAsync(replacement.Id, It.IsAny<CancellationToken>())).ReturnsAsync(replacement);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Permanent("already paid"));
        var paidLink = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), paidLink));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(oldPayment.Id), CancellationToken.None);

        replacement.Status.Should().Be(SubscriptionPaymentStatus.Cancelled);
        replacement.GatewayPaymentLinkId.Should().BeNull(); // the replacement never got a link — the doctor can never pay twice
        oldPayment.Status.Should().NotBe(SubscriptionPaymentStatus.Superseded);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{oldPayment.Id}", nameof(SubscriptionPayment), oldPayment.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Idempotency / terminal states ----

    [Theory]
    [InlineData(SubscriptionPaymentStatus.Superseded)]
    [InlineData(SubscriptionPaymentStatus.Cancelled)]
    [InlineData(SubscriptionPaymentStatus.Expired)]
    public async Task HandleAsync_AlreadyTerminal_IsNoOpAndNeverCallsGateway(SubscriptionPaymentStatus status)
    {
        var payment = CreateLinkedPayment(Guid.NewGuid());
        switch (status)
        {
            case SubscriptionPaymentStatus.Superseded: payment.MarkSuperseded(Guid.NewGuid()); break;
            case SubscriptionPaymentStatus.Cancelled: payment.MarkCancelled(); break;
            case SubscriptionPaymentStatus.Expired: payment.MarkExpired(); break;
        }
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        _paymentGateway.Verify(g => g.CancelPaymentLinkAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TransientCancelError_ThrowsRetryableException()
    {
        var payment = CreateLinkedPayment(Guid.NewGuid());
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Transient("timeout"));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PermanentCancelErrorAndLinkNotActuallyPaid_ThrowsPermanentWorkflowException()
    {
        var payment = CreateLinkedPayment(Guid.NewGuid());
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _paymentGateway.Setup(g => g.CancelPaymentLinkAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync(GatewayResult.Permanent("gone"));
        var cancelledLink = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "cancelled", null, null, null, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), cancelledLink));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_PaymentNotFound_ThrowsPermanentWorkflowException()
    {
        var paymentId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetByIdAsync(paymentId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(paymentId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }
}
