using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class CounterSessionConfiguration : IEntityTypeConfiguration<CounterSession>
{
    public void Configure(EntityTypeBuilder<CounterSession> builder)
    {
        builder.ToTable("CounterSessions", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ApplicationId).IsRequired();
        builder.HasIndex(c => c.ApplicationId);

        builder.Property(c => c.SessionDate).IsRequired();
        builder.Property(c => c.OpenedByUserId).IsRequired();
        builder.Property(c => c.OpenedAt).IsRequired();

        builder.Property(c => c.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(c => c.TotalCollected).IsRequired().HasPrecision(18, 2);
        builder.Property(c => c.TotalExpenses).IsRequired().HasPrecision(18, 2);
        builder.Property(c => c.NetAmount).IsRequired().HasPrecision(18, 2);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(256);
        builder.Property(c => c.UpdatedBy).HasMaxLength(256);
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
