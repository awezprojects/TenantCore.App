using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class AmountHandoverConfiguration : IEntityTypeConfiguration<AmountHandover>
{
    public void Configure(EntityTypeBuilder<AmountHandover> builder)
    {
        builder.ToTable("AmountHandovers", "clinic");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.ApplicationId).IsRequired();
        builder.HasIndex(a => a.ApplicationId);

        builder.Property(a => a.CounterSessionId).IsRequired();
        builder.Property(a => a.HandedOverByUserId).IsRequired();
        builder.Property(a => a.HandedOverToUserId).IsRequired();
        builder.Property(a => a.HandedOverToRoleName).IsRequired().HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(a => a.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.Property(a => a.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(a => a.HandedOverAt).IsRequired();
        builder.Property(a => a.ResolutionNotes).HasMaxLength(500);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(256);
        builder.Property(a => a.UpdatedBy).HasMaxLength(256);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasOne<CounterSession>()
               .WithMany()
               .HasForeignKey(a => a.CounterSessionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
