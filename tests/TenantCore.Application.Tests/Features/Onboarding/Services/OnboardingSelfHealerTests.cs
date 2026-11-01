using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Services;

public class OnboardingSelfHealerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();
    private readonly Mock<IWorkflowTaskRepository> _workflowTaskRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public OnboardingSelfHealerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private OnboardingSelfHealer CreateHealer() => new(_requestRepository.Object, _workflowTaskRepository.Object, _workflowEnqueuer.Object);

    private static ClinicOnboardingRequest CreateRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Clinic", "SUNRISE", "addr", "city", "state", "411001",
        null, null, null, "Doc", "MR1", "MCI", 1, null, null);

    /// <summary>
    /// GetByIdempotencyKeyAsync unconfigured (any key) returns null by default via Moq's Task
    /// auto-fixup — callers that need a specific existing task set it up explicitly per test.
    /// </summary>
    private void SetupNoOpenTasks(Guid requestId) { /* no-op: kept for existing call sites' readability */ }

    [Fact]
    public async Task HealRequestAsync_OpenTaskAlreadyPendingUnderItsExactAggregate_IsNoOp()
    {
        // Regression coverage for the bug where this "already has an open task" check looked
        // under the wrong aggregate (always ClinicOnboardingRequest, even for steps tracked under
        // the request's SubscriptionPayment) and so never actually detected an in-flight task.
        var request = CreateRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var openTask = WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"create-link:{paymentId}", nameof(SubscriptionPayment), paymentId);
        _workflowTaskRepository.Setup(r => r.GetByIdempotencyKeyAsync($"create-link:{paymentId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(openTask);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        openTask.Status.Should().Be(WorkflowTaskStatus.Pending); // untouched — still legitimately in flight
    }

    [Fact]
    public async Task HealRequestAsync_FailedTaskUnderItsExactAggregate_IsResetForRetry_NotSilentlyIgnored()
    {
        // Regression coverage for the bug where a Failed task was never actually revived: the old
        // code's re-enqueue attempt reused the same idempotency key, and EnqueueAsync's "return the
        // existing row unchanged" semantics meant a permanently-Failed task stayed Failed forever.
        var request = CreateRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var failedTask = WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"create-link:{paymentId}", nameof(SubscriptionPayment), paymentId);
        failedTask.TryLease("instance-a", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);
        failedTask.Fail("Razorpay rejected the request");
        _workflowTaskRepository.Setup(r => r.GetByIdempotencyKeyAsync($"create-link:{paymentId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedTask);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        failedTask.Status.Should().Be(WorkflowTaskStatus.Pending);
        failedTask.AttemptCount.Should().Be(0);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealRequestAsync_ApprovedWithCurrentPayment_EnqueuesCreatePaymentLink()
    {
        var request = CreateRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, $"create-link:{paymentId}", nameof(SubscriptionPayment), paymentId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealRequestAsync_PaymentReceivedNotProvisioned_EnqueuesProvisionClinic()
    {
        var request = CreateRequest();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(Guid.NewGuid());
        request.MarkAwaitingPayment();
        request.MarkPaymentReceived();
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealRequestAsync_ProvisioningTrialGrantProvisioned_EnqueuesActivateSubscriptionWithTrialKey()
    {
        var request = CreateRequest();
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test");
        request.SetProvisionedClinic(Guid.NewGuid());
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ActivateSubscription, $"activate-subscription:trial:{request.Id}", nameof(ClinicOnboardingRequest), request.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealRequestAsync_ProvisioningPaidPlanProvisioned_EnqueuesActivateSubscriptionWithPaymentKey()
    {
        var request = CreateRequest();
        var paymentId = Guid.NewGuid();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(paymentId);
        request.MarkAwaitingPayment();
        request.MarkPaymentReceived();
        request.SetProvisionedClinic(Guid.NewGuid());
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ActivateSubscription, $"activate-subscription:{paymentId}", nameof(SubscriptionPayment), paymentId,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HealRequestAsync_AwaitingPayment_DoesNothingAndReturnsFalse()
    {
        var request = CreateRequest();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        request.SetCurrentPayment(Guid.NewGuid());
        request.MarkAwaitingPayment();
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HealRequestAsync_TerminalStatus_DoesNothing()
    {
        var request = CreateRequest();
        request.Cancel(); // terminal
        SetupNoOpenTasks(request.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var healer = CreateHealer();
        await healer.HealRequestAsync(request.Id, CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HealStuckRequestsAsync_CountsOnlyRequestsActuallyHealed()
    {
        var healedRequest = CreateRequest();
        healedRequest.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        healedRequest.SetCurrentPayment(Guid.NewGuid());

        var awaitingRequest = CreateRequest();
        awaitingRequest.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        awaitingRequest.SetCurrentPayment(Guid.NewGuid());
        awaitingRequest.MarkAwaitingPayment(); // not healable — returns false

        _requestRepository.Setup(r => r.GetStuckAsync(It.IsAny<CancellationToken>())).ReturnsAsync([healedRequest, awaitingRequest]);
        SetupNoOpenTasks(healedRequest.Id);
        SetupNoOpenTasks(awaitingRequest.Id);
        _requestRepository.Setup(r => r.GetByIdAsync(healedRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(healedRequest);
        _requestRepository.Setup(r => r.GetByIdAsync(awaitingRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(awaitingRequest);

        var healer = CreateHealer();
        var healedCount = await healer.HealStuckRequestsAsync(CancellationToken.None);

        healedCount.Should().Be(1);
    }
}
