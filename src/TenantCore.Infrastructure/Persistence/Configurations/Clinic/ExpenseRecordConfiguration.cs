using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ExpenseRecordConfiguration : IEntityTypeConfiguration<ExpenseRecord>
{
    public void Configure(EntityTypeBuilder<ExpenseRecord> builder)
    {
        builder.ToTable("ExpenseRecords", "clinic");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ApplicationId).IsRequired();
        builder.HasIndex(e => e.ApplicationId);

        builder.Property(e => e.ExpenseCategoryId).IsRequired();
        builder.Property(e => e.CategoryName).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.PaidAmount).IsRequired().HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.RecordedByUserId).IsRequired();
        builder.Property(e => e.RecordedAt).IsRequired();
        builder.Property(e => e.CounterSessionId);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne<ExpenseCategory>()
               .WithMany()
               .HasForeignKey(e => e.ExpenseCategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
