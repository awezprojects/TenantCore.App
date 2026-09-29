using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.BackgroundJobs;
using TenantCore.Infrastructure.Services;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Tests.BackgroundJobs;

/// <summary>
/// Tests for <see cref="WorkflowTaskProcessor"/> — the durable-outbox poller. ProcessOneAsync,
/// ComputeBackoffSeconds and FailAndFlagAsync are private, so they are exercised via reflection
/// against a processor wired with mocked dependencies (a fresh DI scope per attempt is simulated
/// by a single mocked <see cref="IServiceProvider"/> returned from the mocked scope factory).
/// See plan/clinic-trial-razorpay-subscriptions/PLAN.md "Reliability Design".
/// </summary>
public class WorkflowTaskProcessorTests
{
    // ── Handler dispatch by type ────────────────────────────────────────────

    [Fact]
    public async Task ProcessOneAsync_DispatchesToTheHandlerMatchingTaskType_AndMarksItSucceeded()
    {
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, $"email:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var emailHandler = new FakeHandler(WorkflowTaskType.SendEmail, (_, _) => Task.CompletedTask);
        var paymentLinkHandler = new FakeHandler(WorkflowTaskType.CreatePaymentLink, (_, _) => Task.CompletedTask);
        ctx.Handlers.Add(paymentLinkHandler);
        ctx.Handlers.Add(emailHandler);

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        emailHandler.CallCount.Should().Be(1);
        paymentLinkHandler.CallCount.Should().Be(0);
        task.Status.Should().Be(WorkflowTaskStatus.Succeeded);
        task.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessOneAsync_NoHandlerRegisteredForTaskType_TreatedAsPermanentFailure()
    {
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.ActivateSubscription, $"activate:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        // No handlers registered at all.

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.Status.Should().Be(WorkflowTaskStatus.Failed);
        task.LastError.Should().Contain("No handler registered");
    }

    // ── Transient error → reschedule with backoff ───────────────────────────

    [Fact]
    public async Task ProcessOneAsync_HandlerThrowsOrdinaryException_ReschedulesWithBackoff_DoesNotFail()
    {
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.ConfirmPayment, $"confirm:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.Handlers.Add(new FakeHandler(WorkflowTaskType.ConfirmPayment, (_, _) => throw new InvalidOperationException("Razorpay timed out")));

        var processor = ctx.BuildProcessor();
        var before = DateTime.UtcNow;
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.Status.Should().Be(WorkflowTaskStatus.Pending);
        task.AttemptCount.Should().Be(1);
        task.LastError.Should().Contain("Razorpay timed out");
        task.NextAttemptAt.Should().BeAfter(before);
        ctx.RequestRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Permanent error → fail immediately + flag attention ─────────────────

    [Fact]
    public async Task ProcessOneAsync_HandlerThrowsPermanentWorkflowException_FailsTask_AndFlagsRelatedRequestAttention()
    {
        var ctx = new ProcessorTestContext();
        var request = CreateSubmittedRequest();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id);

        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.RequestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        ctx.RequestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.Handlers.Add(new FakeHandler(WorkflowTaskType.ProvisionClinic, (_, _) => throw new PermanentWorkflowException("clinic code taken")));

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.Status.Should().Be(WorkflowTaskStatus.Failed);
        task.LastError.Should().Be("clinic code taken");

        request.NeedsAttention.Should().BeTrue();
        request.AttentionReason.Should().Contain("ProvisionClinic");
        request.AttentionReason.Should().Contain("clinic code taken");
        ctx.RequestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOneAsync_PermanentFailure_SubscriptionPaymentAggregateWithNoOnboardingRequest_NeverLooksUpARequest()
    {
        // A renewal payment (or one that couldn't even be found) has no OnboardingRequestId to
        // resolve back to — there's genuinely no request to flag.
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.ActivateSubscription, $"activate:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.Handlers.Add(new FakeHandler(WorkflowTaskType.ActivateSubscription, (_, _) => throw new PermanentWorkflowException("duplicate subscription")));

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.Status.Should().Be(WorkflowTaskStatus.Failed);
        ctx.RequestRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOneAsync_PermanentFailure_SubscriptionPaymentAggregateWithOnboardingRequest_FlagsThatRequestAttention()
    {
        // Regression test for the bug where CreatePaymentLink/CancelPaymentLink/ConfirmPayment/
        // paid-ActivateSubscription failures (all tracked under the SubscriptionPayment aggregate,
        // not ClinicOnboardingRequest) never surfaced on the request's NeedsAttention flag.
        var ctx = new ProcessorTestContext();
        var request = CreateSubmittedRequest();
        var payment = SubscriptionPayment.CreateForOnboarding(
            request.Id, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR",
            request.RequesterName, request.RequesterEmail, request.RequesterPhone);
        var task = WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"create-link:{payment.Id}", nameof(SubscriptionPayment), payment.Id);

        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.PaymentRepository.Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        ctx.RequestRepository.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        ctx.RequestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.Handlers.Add(new FakeHandler(WorkflowTaskType.CreatePaymentLink, (_, _) => throw new PermanentWorkflowException("Razorpay rejected the request")));

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.Status.Should().Be(WorkflowTaskStatus.Failed);
        request.NeedsAttention.Should().BeTrue();
        request.AttentionReason.Should().Contain("CreatePaymentLink");
        request.AttentionReason.Should().Contain("Razorpay rejected the request");
    }

    // ── Exhausted attempts → also a terminal Fail(), not another retry ──────

    [Fact]
    public async Task ProcessOneAsync_TransientErrorButAttemptsExhausted_FailsInsteadOfReschedulingAgain()
    {
        var ctx = new ProcessorTestContext();
        // maxAttempts: 1 — TryLease bumps AttemptCount to 1 on this very attempt, so
        // ExceedsMaxAttempts (AttemptCount >= MaxAttempts) is already true when the handler throws.
        var task = WorkflowTask.Enqueue(WorkflowTaskType.SendEmail, $"email:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid(), maxAttempts: 1);
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        ctx.Handlers.Add(new FakeHandler(WorkflowTaskType.SendEmail, (_, _) => throw new InvalidOperationException("smtp down")));

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        task.AttemptCount.Should().Be(1);
        task.ExceedsMaxAttempts.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Failed);
        task.LastError.Should().Contain("smtp down");
    }

    // ── Lease conflict → skip without running the handler ───────────────────

    [Fact]
    public async Task ProcessOneAsync_TaskAlreadyLeasedByAnotherInstance_WithLeaseStillActive_SkipsWithoutRunningHandler()
    {
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"link:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());

        // Another instance leased it moments ago, and that lease has not expired yet.
        task.TryLease("other-instance", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow).Should().BeTrue();

        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var handler = new FakeHandler(WorkflowTaskType.CreatePaymentLink, (_, _) => Task.CompletedTask);
        ctx.Handlers.Add(handler);

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        handler.CallCount.Should().Be(0);
        task.Status.Should().Be(WorkflowTaskStatus.InProgress);
        task.LockedBy.Should().Be("other-instance");
        ctx.TaskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOneAsync_TaskInProgressWithAnExpiredLease_IsRecoveredAndProcessed_CrashRecovery()
    {
        var ctx = new ProcessorTestContext();
        var task = WorkflowTask.Enqueue(WorkflowTaskType.CreatePaymentLink, $"link:{Guid.NewGuid()}", "SubscriptionPayment", Guid.NewGuid());

        // Simulate a previous instance that crashed mid-attempt: it leased the task, but that
        // lease window has already elapsed by the time we look at it again — TryLease must
        // still recover it (the bug this locks in: only Pending used to be leasable).
        task.TryLease("crashed-instance", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow).Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.InProgress);

        ctx.TaskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        ctx.TaskRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new FakeHandler(WorkflowTaskType.CreatePaymentLink, (_, _) => Task.CompletedTask);
        ctx.Handlers.Add(handler);

        var processor = ctx.BuildProcessor();
        await ProcessorTestContext.InvokeProcessOneAsync(processor, task.Id);

        handler.CallCount.Should().Be(1);
        task.Status.Should().Be(WorkflowTaskStatus.Succeeded);
        task.AttemptCount.Should().Be(2); // one from the crashed attempt, one from this recovery
        task.LockedBy.Should().BeNull();
    }

    [Fact]
    public async Task ProcessOneAsync_TaskNoLongerExists_ReturnsWithoutError()
    {
        var ctx = new ProcessorTestContext();
        var missingId = Guid.NewGuid();
        ctx.TaskRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkflowTask?)null);

        var processor = ctx.BuildProcessor();
        var act = () => ProcessorTestContext.InvokeProcessOneAsync(processor, missingId);

        await act.Should().NotThrowAsync();
        ctx.TaskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Backoff schedule + jitter bounds ─────────────────────────────────────

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(4, 300)]
    [InlineData(5, 900)]
    [InlineData(6, 1800)]
    [InlineData(7, 3600)]
    [InlineData(8, 3600)]  // beyond the configured schedule → clamps to the last (hourly) entry
    [InlineData(50, 3600)]
    public void ComputeBackoffSeconds_MapsAttemptToScheduleEntry_WithJitterWithinTwentyPercent(int attemptNumber, int expectedBaseDelay)
    {
        var ctx = new ProcessorTestContext();
        var processor = ctx.BuildProcessor();
        var jitterRange = Math.Max(expectedBaseDelay / 5, 1);
        var minExpected = Math.Max(expectedBaseDelay - jitterRange, 1);
        var maxExpected = expectedBaseDelay + jitterRange;

        for (var i = 0; i < 100; i++)
        {
            var delay = ProcessorTestContext.InvokeComputeBackoffSeconds(processor, attemptNumber);
            delay.Should().BeInRange(minExpected, maxExpected);
        }
    }

    [Fact]
    public void ComputeBackoffSeconds_AttemptZeroOrBelow_ClampsToTheFirstScheduleEntry()
    {
        var ctx = new ProcessorTestContext();
        var processor = ctx.BuildProcessor();

        var delay = ProcessorTestContext.InvokeComputeBackoffSeconds(processor, 0);

        delay.Should().BeInRange(24, 36); // 30s ± 20%
    }

    [Fact]
    public void ComputeBackoffSeconds_EmptySchedule_FallsBackToSixtySeconds()
    {
        var ctx = new ProcessorTestContext { Settings = new WorkflowOptions { BackoffScheduleSeconds = [] } };
        var processor = ctx.BuildProcessor();

        var delay = ProcessorTestContext.InvokeComputeBackoffSeconds(processor, 5);

        delay.Should().Be(60);
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static ClinicOnboardingRequest CreateSubmittedRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Dr Test", "doctor@test.example", "9999999999",
        "Test Clinic", "TESTCODE", "123 Test Street", "Test City", "Test State", "123456",
        null, null, null, "Dr Test", "MRN-1", "Test Medical Council", 5, null, null);

    private sealed class FakeHandler(WorkflowTaskType taskType, Func<WorkflowTask, CancellationToken, Task> handle) : IWorkflowTaskHandler
    {
        public WorkflowTaskType TaskType { get; } = taskType;
        public int CallCount { get; private set; }

        public Task HandleAsync(WorkflowTask task, CancellationToken ct)
        {
            CallCount++;
            return handle(task, ct);
        }
    }

    /// <summary>
    /// Wires a WorkflowTaskProcessor with mocked dependencies resolved from a single mocked
    /// scope/provider (standing in for the "fresh DI scope" the processor creates per attempt),
    /// and exposes the processor's private members via reflection for direct, deterministic tests.
    /// </summary>
    private sealed class ProcessorTestContext
    {
        public Mock<IWorkflowTaskRepository> TaskRepository { get; } = new();
        public Mock<IClinicOnboardingRequestRepository> RequestRepository { get; } = new();
        public Mock<ISubscriptionPaymentRepository> PaymentRepository { get; } = new();
        public Mock<IActionLogger> ActionLogger { get; } = new();
        public List<IWorkflowTaskHandler> Handlers { get; } = [];
        public WorkflowOptions Settings { get; set; } = new();

        public ProcessorTestContext()
        {
            // Default: no SubscriptionPayment found, so ResolveRelatedRequestIdAsync short-circuits
            // to null for the common "aggregate is SubscriptionPayment but no request behind it"
            // case (e.g. a renewal, or a test that isn't exercising the flag-the-request path at
            // all). Set here, in the constructor, so a test's own more-specific Setup (configured
            // before calling BuildProcessor()) correctly takes precedence over this blanket default
            // — Moq resolves overlapping setups on the same mock by "last configured wins".
            PaymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        }

        public WorkflowTaskProcessor BuildProcessor()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IWorkflowTaskRepository))).Returns(TaskRepository.Object);
            serviceProvider.Setup(p => p.GetService(typeof(IActionLogger))).Returns(ActionLogger.Object);
            serviceProvider.Setup(p => p.GetService(typeof(IClinicOnboardingRequestRepository))).Returns(RequestRepository.Object);
            serviceProvider.Setup(p => p.GetService(typeof(ISubscriptionPaymentRepository))).Returns(PaymentRepository.Object);
            serviceProvider.Setup(p => p.GetService(typeof(IWorkflowCorrelationContext))).Returns(new WorkflowCorrelationContext());
            serviceProvider.Setup(p => p.GetService(typeof(IEnumerable<IWorkflowTaskHandler>))).Returns(Handlers);

            var scope = new Mock<IServiceScope>();
            scope.SetupGet(s => s.ServiceProvider).Returns(serviceProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            return new WorkflowTaskProcessor(scopeFactory.Object, Options.Create(Settings), NullLogger<WorkflowTaskProcessor>.Instance);
        }

        public static Task InvokeProcessOneAsync(WorkflowTaskProcessor processor, Guid taskId)
        {
            var method = typeof(WorkflowTaskProcessor).GetMethod("ProcessOneAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task)method.Invoke(processor, [taskId, CancellationToken.None])!;
        }

        public static int InvokeComputeBackoffSeconds(WorkflowTaskProcessor processor, int attemptNumber)
        {
            var method = typeof(WorkflowTaskProcessor).GetMethod("ComputeBackoffSeconds", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (int)method.Invoke(processor, [attemptNumber])!;
        }
    }
}
