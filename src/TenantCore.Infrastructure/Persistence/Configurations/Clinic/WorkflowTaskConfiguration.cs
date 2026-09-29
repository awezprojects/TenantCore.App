using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("WorkflowTasks", "clinic");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TaskType).IsRequired().HasConversion<int>();
        builder.Property(t => t.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(t => t.AggregateType).IsRequired().HasMaxLength(50);
        builder.Property(t => t.AggregateId).IsRequired();
        builder.Property(t => t.PayloadJson).HasColumnType("nvarchar(max)");

        builder.Property(t => t.Status).IsRequired().HasConversion<int>();
        builder.Property(t => t.AttemptCount).IsRequired();
        builder.Property(t => t.MaxAttempts).IsRequired();
        builder.Property(t => t.NextAttemptAt).IsRequired();

        builder.Property(t => t.LockedUntil);
        builder.Property(t => t.LockedBy).HasMaxLength(100);
        builder.Property(t => t.LastError).HasMaxLength(2000);
        builder.Property(t => t.CompletedAt);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(t => t.IdempotencyKey).IsUnique();
        // The processor's poll query: Pending tasks whose NextAttemptAt has passed.
        builder.HasIndex(t => new { t.Status, t.NextAttemptAt });
        builder.HasIndex(t => new { t.AggregateType, t.AggregateId });
    }
}
