using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class OpdParticularConfiguration : IEntityTypeConfiguration<OpdParticular>
{
    public void Configure(EntityTypeBuilder<OpdParticular> builder)
    {
        builder.ToTable("OpdParticulars", "clinic");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.ApplicationId).IsRequired();
        builder.HasIndex(o => o.ApplicationId);

        builder.Property(o => o.OpdRegistrationId).IsRequired();
        builder.HasIndex(o => o.OpdRegistrationId);

        builder.Property(o => o.ParticularId).IsRequired();
        builder.Property(o => o.ParticularName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.Amount).IsRequired().HasPrecision(18, 2);

        // Per-item payment tracking
        builder.Property(o => o.PaymentStatus).IsRequired();
        builder.Property(o => o.CollectedAt);
        builder.Property(o => o.CollectedByUserId);
        builder.Property(o => o.CounterSessionId);
        builder.HasIndex(o => o.CounterSessionId);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(256);
        builder.Property(o => o.UpdatedBy).HasMaxLength(256);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne<OpdRegistration>()
               .WithMany()
               .HasForeignKey(o => o.OpdRegistrationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Particular>()
               .WithMany()
               .HasForeignKey(o => o.ParticularId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
