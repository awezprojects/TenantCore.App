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

public class ConfirmPaymentTaskHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ConfirmPaymentTaskHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ConfirmPaymentTaskHandler CreateHandler() =>
        new(_paymentRepository.Object, _requestRepository.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

    private static SubscriptionPayment CreateLinkedOnboardingPayment(Guid requestId, decimal amount = 999m)
    {
        var payment = SubscriptionPayment.CreateForOnboarding(requestId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", amount, amount, "INR", "Dr. Jane", "jane@example.test", "9876543210");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        return payment;
    }

    private static ClinicOnboardingRequest CreateAwaitingPaymentRequest(Guid paymentId, decimal amount = 999m)
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, amount, amount, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        request.MarkAwaitingPayment();
        return request;
    }

    private static WorkflowTask CreateTask(Guid paymentId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.ConfirmPayment, $"confirm-payment:{paymentId}", nameof(SubscriptionPayment), paymentId);

    [Fact]
    public async Task HandleAsync_OnboardingPaid_MarksPaidMovesRequestToProvisioningAndEnqueuesProvisionClinic()
    {
        var payment = CreateLinkedOnboardingPayment(Guid.Empty, 999m);
        var request = CreateAwaitingPaymentRequest(payment.Id, 999m);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        // Route lookups keyed by OnboardingRequestId back to the same request instance.
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RenewalPaid_MarksPaidAndEnqueuesActivateSubscription()
    {
        var payment = SubscriptionPayment.CreateForRenewal(Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, "INR", "Admin", "admin@clinic.test", "9876543210", Guid.NewGuid());
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "card", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ActivateSubscription, $"activate-subscription:{payment.Id}", nameof(SubscriptionPayment), payment.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AmountMismatch_IsPermanentAndFlagsAttention()
    {
        var payment = CreateLinkedOnboardingPayment(Guid.Empty, 999m);
        var request = CreateAwaitingPaymentRequest(payment.Id, 999m);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 500m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
        request.NeedsAttention.Should().BeTrue();
        payment.Status.Should().NotBe(SubscriptionPaymentStatus.Paid);
    }

    [Fact]
    public async Task HandleAsync_LinkNotPaidYet_ThrowsRetryableException()
    {
        var payment = CreateLinkedOnboardingPayment(Guid.Empty);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "created", null, null, null, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().NotBeOfType<PermanentWorkflowException>();
    }

    [Fact]
    public async Task HandleAsync_RejectedRequest_FlagsRefundRequiredAndDoesNotProvision()
    {
        var payment = CreateLinkedOnboardingPayment(Guid.Empty);
        var request = CreateAwaitingPaymentRequest(payment.Id);
        request.Reject("closed meanwhile", Guid.NewGuid(), "admin@example.test");
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var link = new PaymentLinkInfo("plink_1", "https://razorpay.test/pay/plink_1", "paid", "pay_1", "upi", 999m, DateTime.UtcNow.AddDays(7));
        _paymentGateway.Setup(g => g.GetPaymentLinkByIdAsync("plink_1", It.IsAny<CancellationToken>())).ReturnsAsync((GatewayResult.Ok(), link));

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        request.NeedsAttention.Should().BeTrue();
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyPaid_IsIdempotentRegardlessOfHowFarTheRequestHasAdvanced()
    {
        // Locks in: a repeat ConfirmPayment (webhook racing reconciliation, or a retried attempt)
        // must never throw even though the request has already moved past PaymentReceived to Active.
        var payment = CreateLinkedOnboardingPayment(Guid.Empty);
        payment.MarkPaid("pay_1", "upi");
        var request = CreateAwaitingPaymentRequest(payment.Id);
        request.MarkPaymentReceived();
        request.MarkProvisioning();
        request.SetProvisionedClinic(Guid.NewGuid());
        request.Activate(Guid.NewGuid()); // now Active — fully terminal-success

        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        await action.Should().NotThrowAsync();
        request.Status.Should().Be(ClinicOnboardingStatus.Active);
        // Never re-fetches Razorpay once already marked Paid.
        _paymentGateway.Verify(g => g.GetPaymentLinkByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyPaidButRequestStillAwaitingPayment_ReEnqueuesProvisioningSafetyNet()
    {
        // The earlier attempt crashed after MarkPaid but before the downstream enqueue committed.
        var payment = CreateLinkedOnboardingPayment(Guid.Empty);
        payment.MarkPaid("pay_1", "upi");
        var request = CreateAwaitingPaymentRequest(payment.Id); // still AwaitingPayment
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(payment.Id), CancellationToken.None);

        await action.Should().NotThrowAsync();
        request.Status.Should().Be(ClinicOnboardingStatus.Provisioning);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, It.IsAny<string>(), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
