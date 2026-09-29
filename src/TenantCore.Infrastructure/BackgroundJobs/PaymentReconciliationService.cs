using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.BackgroundJobs;

/// <summary>
/// Every ReconciliationIntervalMinutes: (1) asks Razorpay directly about every payment still
/// LinkCreated for more than 10 minutes, so the system stays correct even if a webhook is lost
/// or Razorpay disables it; (2) runs the self-healing sweep so a bug or manual DB edit can never
/// leave a request permanently stuck with no open task.
/// </summary>
public sealed class PaymentReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkflowOptions> options,
    IConfiguration configuration,
    ILogger<PaymentReconciliationService> logger) : BackgroundService
{
    // Single-instance-safe throttle for the "webhook never arrived" alert. A multi-instance
    // deployment could send more than one alert per hour across instances — acceptable, since
    // this is an informational alert, not a correctness guard.
    private static DateTime? _lastSilentWebhookAlertAt;
    private static readonly object AlertLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("PaymentReconciliationService disabled via Workflow:Enabled=false.");
            return;
        }

        try
        {
            // Let startup migrations/seeding finish before the first sweep.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcilePaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment reconciliation sweep failed unexpectedly.");
            }

            try
            {
                await HealStuckRequestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Self-healing sweep failed unexpectedly.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(options.Value.ReconciliationIntervalMinutes, 1)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task ReconcilePaymentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var paymentRepository = services.GetRequiredService<ISubscriptionPaymentRepository>();
        var requestRepository = services.GetRequiredService<IClinicOnboardingRequestRepository>();
        var gateway = services.GetRequiredService<IPaymentGateway>();
        var workflowEnqueuer = services.GetRequiredService<IWorkflowEnqueuer>();

        if (!gateway.IsConfigured)
            return;

        var due = await paymentRepository.GetDueForReconciliationAsync(DateTime.UtcNow.AddMinutes(-10), 50, ct);
        if (due.Count == 0)
            return;

        logger.LogInformation("Reconciliation sweep checking {Count} payment(s) with Razorpay.", due.Count);

        // Job run → ActionLogs (ADR-011): only sweeps that had work, so an idle job adds no rows.
        var actionLogger = services.GetRequiredService<IActionLogger>();
        var runId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int paid = 0, expired = 0, cancelled = 0, unreachable = 0;
        var requestType = $"PaymentReconciliationService | due={due.Count}";
        await actionLogger.LogStartedAsync(runId, "Job: Payment Reconciliation", requestType, null, null, ct);

        try
        {
            foreach (var payment in due)
            {
                if (string.IsNullOrEmpty(payment.GatewayPaymentLinkId))
                    continue;

                var (result, link) = await gateway.GetPaymentLinkByIdAsync(payment.GatewayPaymentLinkId, ct);
                if (!result.Success || link == null)
                {
                    unreachable++;
                    continue; // transient — try again next sweep
                }

                switch (link.Status)
                {
                    case "paid":
                        paid++;
                        logger.LogWarning(
                            "Reconciliation found payment {PaymentId} paid at Razorpay with no webhook processed yet — enqueuing ConfirmPayment and alerting ops.",
                            payment.Id);
                        await workflowEnqueuer.EnqueueAsync(
                            WorkflowTaskType.ConfirmPayment, $"confirm-payment:{payment.Id}", nameof(SubscriptionPayment), payment.Id, ct: ct);
                        await MaybeAlertSilentWebhookAsync(workflowEnqueuer, payment.Id, ct);
                        break;

                    case "expired":
                        expired++;
                        payment.MarkExpired();
                        if (payment.Purpose == PaymentPurpose.Onboarding && payment.OnboardingRequestId.HasValue)
                        {
                            var request = await requestRepository.GetByIdAsync(payment.OnboardingRequestId.Value, ct);
                            request?.MarkLinkExpired();
                        }
                        break;

                    case "cancelled":
                        cancelled++;
                        payment.MarkCancelled();
                        break;
                }
            }

            await paymentRepository.SaveChangesAsync(ct);
            await requestRepository.SaveChangesAsync(ct);

            stopwatch.Stop();
            await actionLogger.LogCompletedAsync(runId, "Job: Payment Reconciliation",
                $"{requestType}; paidWithoutWebhook={paid}; expired={expired}; cancelled={cancelled}; gatewayUnreachable={unreachable}",
                null, null, stopwatch.ElapsedMilliseconds, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            await actionLogger.LogFailedAsync(runId, "Job: Payment Reconciliation", requestType, null, null, stopwatch.ElapsedMilliseconds, ex.Message, ct);
            throw;   // ExecuteAsync logs it (→ ApiErrorLogs via the error sink) and retries next interval
        }
    }

    private async Task MaybeAlertSilentWebhookAsync(IWorkflowEnqueuer workflowEnqueuer, Guid paymentId, CancellationToken ct)
    {
        var opsEmail = configuration["Onboarding:OpsNotificationEmail"];
        if (string.IsNullOrWhiteSpace(opsEmail))
            return;

        lock (AlertLock)
        {
            if (_lastSilentWebhookAlertAt.HasValue && DateTime.UtcNow - _lastSilentWebhookAlertAt.Value < TimeSpan.FromHours(1))
                return;
            _lastSilentWebhookAlertAt = DateTime.UtcNow;
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(new { template = "SilentWebhookAlert", to = opsEmail, paymentId });
        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"silent-webhook-alert:{paymentId}:{DateTime.UtcNow:yyyyMMddHHmm}", nameof(SubscriptionPayment), paymentId, payload, ct);
    }

    private async Task HealStuckRequestsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var healer = scope.ServiceProvider.GetRequiredService<IOnboardingSelfHealer>();
        var healed = await healer.HealStuckRequestsAsync(ct);
        if (healed > 0)
        {
            logger.LogInformation("Self-healing sweep re-enqueued the next step for {Count} stuck request(s).", healed);

            // Only sweeps that healed something reach ActionLogs (ADR-011).
            var actionLogger = scope.ServiceProvider.GetRequiredService<IActionLogger>();
            await actionLogger.LogCompletedAsync(Guid.NewGuid(), "Job: Onboarding Self-Heal",
                $"OnboardingSelfHealer | reEnqueued={healed}", null, null, 0, ct);
        }
    }
}
