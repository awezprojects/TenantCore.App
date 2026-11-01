using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class WorkflowTaskRepository(ClinicDbContext dbContext)
    : ClinicRepository<WorkflowTask>(dbContext), IWorkflowTaskRepository
{
    /// <summary>
    /// Returns the existing row instead of adding a duplicate when a task with this
    /// IdempotencyKey already exists. This check-then-add is not itself atomic; the unique
    /// index on IdempotencyKey is the final backstop against a genuine cross-instance race,
    /// and the state transition each enqueue accompanies is protected by RowVersion optimistic
    /// concurrency on the aggregate row saved in the same unit of work.
    /// </summary>
    public async Task<WorkflowTask> EnqueueAsync(WorkflowTask task, CancellationToken ct = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(t => t.IdempotencyKey == task.IdempotencyKey, ct);
        if (existing != null)
            return existing;

        await DbSet.AddAsync(task, ct);
        return task;
    }

    /// <summary>
    /// Due Pending tasks, plus InProgress tasks whose lease has expired — the latter is how a
    /// task abandoned by a crashed instance gets picked up again instead of stalling forever.
    /// </summary>
    public async Task<IReadOnlyList<WorkflowTask>> GetDueAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
        => await DbSet
            .Where(t =>
                (t.Status == WorkflowTaskStatus.Pending && t.NextAttemptAt <= utcNow) ||
                (t.Status == WorkflowTaskStatus.InProgress && (t.LockedUntil == null || t.LockedUntil <= utcNow)))
            .OrderBy(t => t.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkflowTask>> GetOpenForAggregateAsync(string aggregateType, Guid aggregateId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(t => t.AggregateType == aggregateType && t.AggregateId == aggregateId
                     && (t.Status == WorkflowTaskStatus.Pending || t.Status == WorkflowTaskStatus.InProgress))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkflowTask>> GetFailedForAggregateAsync(string aggregateType, Guid aggregateId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(t => t.AggregateType == aggregateType && t.AggregateId == aggregateId && t.Status == WorkflowTaskStatus.Failed)
            .ToListAsync(ct);

    public async Task<WorkflowTask?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
}
