using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicUsageRecordConfiguration : IEntityTypeConfiguration<ClinicUsageRecord>
{
    public void Configure(EntityTypeBuilder<ClinicUsageRecord> builder)
    {
        builder.ToTable("ClinicUsageRecords", "clinic");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.ApplicationId).IsRequired();
        builder.Property(u => u.BillingItem).IsRequired().HasConversion<int>();
        builder.Property(u => u.SourceEntityId).IsRequired();
        builder.Property(u => u.Quantity).IsRequired();
        builder.Property(u => u.UnitRate).HasPrecision(18, 4).IsRequired();
        builder.Property(u => u.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(u => u.PayerMode).IsRequired().HasConversion<int>();
        builder.Property(u => u.IsFree).IsRequired();
        builder.Property(u => u.ChargedAt).IsRequired();

        // The idempotency guarantee for the whole design: a retried request, a duplicated workflow
        // task or a replayed call can never produce a second charge for the same operation.
        builder.HasIndex(u => new { u.ApplicationId, u.BillingItem, u.SourceEntityId }).IsUnique();

        // The usage dashboard's range query, and its per-item grouping. A given IpdRegistration id
        // appears twice by design — once as IpdAdmission, once as IpdDischarge — which the unique
        // index above permits because BillingItem differs.
        builder.HasIndex(u => new { u.ApplicationId, u.ChargedAt });
        builder.HasIndex(u => new { u.ApplicationId, u.BillingItem, u.ChargedAt });

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.UpdatedBy).HasMaxLength(256);
        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}
