using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Tasks;

public class ProcessWebhookEventTaskHandlerTests
{
    private readonly Mock<IPaymentWebhookEventRepository> _webhookEventRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public ProcessWebhookEventTaskHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _webhookEventRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ProcessWebhookEventTaskHandler CreateHandler() =>
        new(_webhookEventRepository.Object, _paymentRepository.Object, _requestRepository.Object, _workflowEnqueuer.Object);

    private static string BuildPayload(string eventType, Guid paymentId) =>
        "{\"event\":\"" + eventType + "\",\"payload\":{\"payment_link\":{\"entity\":{\"reference_id\":\"" + paymentId.ToString("N") + "\"}}}}";

    private static WorkflowTask CreateTask(Guid webhookEventId) =>
        WorkflowTask.Enqueue(WorkflowTaskType.ProcessWebhookEvent, $"process-webhook:{webhookEventId}", nameof(PaymentWebhookEvent), webhookEventId);

    [Fact]
    public async Task HandleAsync_PaymentLinkPaidEvent_EnqueuesConfirmPayment()
    {
        var paymentId = Guid.NewGuid();
        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", "evt_1", "payment_link.paid", BuildPayload("payment_link.paid", paymentId));
        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(webhookEvent.Id), CancellationToken.None);

        webhookEvent.ProcessedAt.Should().NotBeNull();
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{paymentId}", nameof(SubscriptionPayment), paymentId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PaymentLinkExpiredEvent_MarksExpiredAndRequestLinkExpiredAndSendsEmail()
    {
        var request = ClinicOnboardingRequest.Submit(
            Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
            null, null, null, "Doc", "MR1", "MCI", 1, null, null);
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var payment = SubscriptionPayment.CreateForOnboarding(request.Id, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr", "dr@example.test", "9876543210");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        request.SetCurrentPayment(payment.Id);
        request.MarkAwaitingPayment();

        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", "evt_2", "payment_link.expired", BuildPayload("payment_link.expired", payment.Id));
        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(webhookEvent.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Expired);
        request.Status.Should().Be(ClinicOnboardingStatus.PaymentLinkExpired);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail, It.Is<string>(k => k.Contains("link-expired")), nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PaymentLinkCancelledEvent_MarksPaymentCancelled()
    {
        var payment = SubscriptionPayment.CreateForOnboarding(Guid.NewGuid(), Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr", "dr@example.test", "9876543210");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));
        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", "evt_3", "payment_link.cancelled", BuildPayload("payment_link.cancelled", payment.Id));

        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);
        _paymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(webhookEvent.Id), CancellationToken.None);

        payment.Status.Should().Be(SubscriptionPaymentStatus.Cancelled);
        _requestRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnknownEventType_MarksProcessedOnlyAndEnqueuesNothing()
    {
        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", "evt_4", "refund.processed", """{"event":"refund.processed"}""");
        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(webhookEvent.Id), CancellationToken.None);

        webhookEvent.ProcessedAt.Should().NotBeNull();
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyProcessed_IsIdempotentNoOp()
    {
        var webhookEvent = PaymentWebhookEvent.Create("Razorpay", "evt_5", "payment_link.paid", BuildPayload("payment_link.paid", Guid.NewGuid()));
        webhookEvent.MarkProcessed();
        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);

        var handler = CreateHandler();
        await handler.HandleAsync(CreateTask(webhookEvent.Id), CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookEventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EventNotFound_ThrowsPermanentWorkflowException()
    {
        var webhookEventId = Guid.NewGuid();
        _webhookEventRepository.Setup(r => r.GetByIdAsync(webhookEventId, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentWebhookEvent?)null);

        var handler = CreateHandler();
        var action = () => handler.HandleAsync(CreateTask(webhookEventId), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentWorkflowException>();
    }
}
