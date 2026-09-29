namespace TenantCore.Application.Common.Workflow;

/// <summary>
/// Configuration for the durable workflow — bound from the "Workflow" section. Lives in
/// Application (not Infrastructure, despite owning the background services) because
/// WorkflowEnqueuer needs it too, and Application can never reference Infrastructure.
/// </summary>
public sealed class WorkflowOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int LeaseMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 72;

    /// <summary>Exponential backoff schedule in seconds; the last value repeats once exhausted (30s, 1m, 2m, 5m, 15m, 30m, then hourly).</summary>
    public int[] BackoffScheduleSeconds { get; set; } = [30, 60, 120, 300, 900, 1800, 3600];

    public int ReconciliationIntervalMinutes { get; set; } = 15;
}
