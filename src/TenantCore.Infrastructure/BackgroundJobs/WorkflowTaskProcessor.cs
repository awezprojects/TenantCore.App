using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using WaitingEx = TenantCore.Domain.Exceptions.WaitingWorkflowException;

namespace TenantCore.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls WorkflowTasks for due work, leases each one (so only one App instance ever runs a
/// given task), dispatches to the matching <see cref="IWorkflowTaskHandler"/> in a fresh DI
/// scope, and classifies the outcome: success, a permanent failure (stop retrying, flag the
/// related request), or a transient failure (reschedule with backoff). See plan's
/// "Reliability Design" for the full rationale.
/// </summary>
public sealed class WorkflowTaskProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkflowOptions> options,
    ILogger<WorkflowTaskProcessor> logger) : BackgroundService
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("WorkflowTaskProcessor disabled via Workflow:Enabled=false.");
            return;
        }

        logger.LogInformation("WorkflowTaskProcessor started — instance {InstanceId}, polling every {Seconds}s.", _instanceId, options.Value.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WorkflowTaskProcessor batch failed unexpectedly — will retry next poll.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested — exit the loop.
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        List<Guid> dueTaskIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var taskRepository = scope.ServiceProvider.GetRequiredService<IWorkflowTaskRepository>();
            var due = await taskRepository.GetDueAsync(options.Value.BatchSize, DateTime.UtcNow, ct);
            dueTaskIds = due.Select(t => t.Id).ToList();
        }

        foreach (var taskId in dueTaskIds)
        {
            if (ct.IsCancellationRequested)
                break;

            await ProcessOneAsync(taskId, ct);
        }
    }

    private async Task ProcessOneAsync(Guid taskId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var taskRepository = services.GetRequiredService<IWorkflowTaskRepository>();
        var actionLogger = services.GetRequiredService<IActionLogger>();

        var task = await taskRepository.GetByIdAsync(taskId, ct);
        if (task == null)
            return;

        var leaseUntil = DateTime.UtcNow.AddMinutes(options.Value.LeaseMinutes);
        if (!task.TryLease(_instanceId, leaseUntil, DateTime.UtcNow))
            return; // another instance holds it, or it is no longer actually due

        await taskRepository.SaveChangesAsync(ct);

        var handler = services.GetServices<IWorkflowTaskHandler>().FirstOrDefault(h => h.TaskType == task.TaskType);
        var correlationId = Guid.NewGuid();

        // Available to any collaborator resolved from this same scope (e.g. AuthProvisioningService)
        // that needs to tag an outgoing call with this attempt's correlation id — the background
        // worker has no HttpContext to draw one from otherwise.
        services.GetRequiredService<IWorkflowCorrelationContext>().CorrelationId = correlationId;

        var stopwatch = Stopwatch.StartNew();

        await SafeLogAsync(() => actionLogger.LogStartedAsync(correlationId, $"Workflow:{task.TaskType}", nameof(WorkflowTask), null, null, ct));

        try
        {
            if (handler == null)
                throw new PermanentWorkflowException($"No handler registered for task type {task.TaskType}.");

            await handler.HandleAsync(task, ct);

            task.Succeed();
            await taskRepository.SaveChangesAsync(ct);

            await SafeLogAsync(() => actionLogger.LogCompletedAsync(correlationId, $"Workflow:{task.TaskType}", nameof(WorkflowTask), null, null, stopwatch.ElapsedMilliseconds, ct));
        }
        catch (PermanentWorkflowException ex)
        {
            logger.LogWarning("WorkflowTask {TaskId} ({TaskType}) failed permanently: {Error}", task.Id, task.TaskType, ex.Message);
            await FailAndFlagAsync(services, task, ex.Message, ct);
            await SafeLogAsync(() => actionLogger.LogFailedAsync(correlationId, $"Workflow:{task.TaskType}", nameof(WorkflowTask), null, null, stopwatch.ElapsedMilliseconds, ex.Message, ct));
        }
        catch (WaitingEx ex)
        {
            // A predicted waiting state (e.g. payment link not yet paid) — retry on the normal
            // backoff schedule but stay quiet: no stack-trace, no alarm, just a debug note.
            logger.LogDebug("WorkflowTask {TaskId} ({TaskType}) waiting (attempt {Attempt}): {Reason}", task.Id, task.TaskType, task.AttemptCount, ex.Message);

            if (task.ExceedsMaxAttempts)
            {
                await FailAndFlagAsync(services, task, ex.Message, ct);
            }
            else
            {
                var delaySeconds = ComputeBackoffSeconds(task.AttemptCount);
                task.ScheduleRetry(ex.Message, DateTime.UtcNow.AddSeconds(delaySeconds));
                await taskRepository.SaveChangesAsync(ct);
            }

            await SafeLogAsync(() => actionLogger.LogFailedAsync(correlationId, $"Workflow:{task.TaskType}", nameof(WorkflowTask), null, null, stopwatch.ElapsedMilliseconds, ex.Message, ct));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WorkflowTask {TaskId} ({TaskType}) attempt {Attempt} failed transiently.", task.Id, task.TaskType, task.AttemptCount);

            if (task.ExceedsMaxAttempts)
            {
                await FailAndFlagAsync(services, task, ex.Message, ct);
            }
            else
            {
                var delaySeconds = ComputeBackoffSeconds(task.AttemptCount);
                task.ScheduleRetry(ex.Message, DateTime.UtcNow.AddSeconds(delaySeconds));
                await taskRepository.SaveChangesAsync(ct);
            }

            await SafeLogAsync(() => actionLogger.LogFailedAsync(correlationId, $"Workflow:{task.TaskType}", nameof(WorkflowTask), null, null, stopwatch.ElapsedMilliseconds, ex.Message, ct));
        }
    }

    private static async Task FailAndFlagAsync(IServiceProvider services, WorkflowTask task, string error, CancellationToken ct)
    {
        var taskRepository = services.GetRequiredService<IWorkflowTaskRepository>();
        task.Fail(error);
        await taskRepository.SaveChangesAsync(ct);

        var requestId = await ResolveRelatedRequestIdAsync(services, task, ct);
        if (requestId == null)
            return;

        var requestRepository = services.GetRequiredService<IClinicOnboardingRequestRepository>();
        var request = await requestRepository.GetByIdAsync(requestId.Value, ct);
        if (request != null)
        {
            request.FlagAttention($"{task.TaskType} failed: {error}");
            await requestRepository.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// A failing task's aggregate is either the ClinicOnboardingRequest directly (ProvisionClinic,
    /// trial ActivateSubscription) or its current SubscriptionPayment (CreatePaymentLink,
    /// CancelPaymentLink, ConfirmPayment, paid ActivateSubscription) — resolve either back to the
    /// request so NeedsAttention always surfaces regardless of which aggregate the task is tracked
    /// under. A renewal payment has no OnboardingRequestId, which correctly yields null here (a
    /// renewal has no onboarding request to flag).
    /// </summary>
    private static async Task<Guid?> ResolveRelatedRequestIdAsync(IServiceProvider services, WorkflowTask task, CancellationToken ct)
    {
        if (task.AggregateType == nameof(ClinicOnboardingRequest))
            return task.AggregateId;

        if (task.AggregateType == nameof(SubscriptionPayment))
        {
            var paymentRepository = services.GetRequiredService<ISubscriptionPaymentRepository>();
            var payment = await paymentRepository.GetByIdAsync(task.AggregateId, ct);
            return payment?.OnboardingRequestId;
        }

        return null;
    }

    private int ComputeBackoffSeconds(int attemptNumber)
    {
        var schedule = options.Value.BackoffScheduleSeconds;
        if (schedule.Length == 0)
            return 60;

        var index = Math.Clamp(attemptNumber - 1, 0, schedule.Length - 1);
        var baseDelay = schedule[index];

        // +/- 20% jitter so many tasks scheduled for the same instant don't all retry together.
        var jitterRange = Math.Max(baseDelay / 5, 1);
        var jitter = Random.Shared.Next(-jitterRange, jitterRange + 1);
        return Math.Max(baseDelay + jitter, 1);
    }

    private async Task SafeLogAsync(Func<Task> logCall)
    {
        try
        {
            await logCall();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Action-log write failed — processing continues regardless.");
        }
    }
}
