using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicWalletTransactionConfiguration : IEntityTypeConfiguration<ClinicWalletTransaction>
{
    public void Configure(EntityTypeBuilder<ClinicWalletTransaction> builder)
    {
        builder.ToTable("ClinicWalletTransactions", "clinic");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.ApplicationId).IsRequired();
        builder.Property(t => t.Type).IsRequired().HasConversion<int>();
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.ReferenceType).IsRequired().HasConversion<int>();
        builder.Property(t => t.Description).IsRequired().HasMaxLength(250);
        builder.Property(t => t.OccurredAt).IsRequired();
        builder.Property(t => t.CreatedByAdminEmail).HasMaxLength(256);

        // The idempotency guarantee: a top-up confirmation, a usage charge or an admin credit can
        // never move money twice for the same reference. Filtered because an adjustment may be
        // legitimately reference-free, and SQL Server treats NULLs as equal in a unique index.
        builder.HasIndex(t => new { t.ApplicationId, t.ReferenceType, t.ReferenceId })
            .IsUnique()
            .HasFilter("[ReferenceId] IS NOT NULL");

        // The wallet ledger page reads newest-first within one clinic.
        builder.HasIndex(t => new { t.ApplicationId, t.OccurredAt }).IsDescending(false, true);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);
        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}
