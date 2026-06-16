using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations;

internal sealed class PregnancyTenureConfiguration : IEntityTypeConfiguration<PregnancyTenure>
{
    public void Configure(EntityTypeBuilder<PregnancyTenure> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.PatientId).IsRequired();
        builder.Property(t => t.ApplicationId).IsRequired();
        builder.Property(t => t.Lmp).IsRequired();
        builder.Property(t => t.EddByLmp).IsRequired();
        builder.Property(t => t.EddByUsg).IsRequired(false);
        builder.Property(t => t.Status).IsRequired().HasDefaultValue(PregnancyTenureStatus.Active);
        builder.Property(t => t.Outcome).IsRequired(false);
        builder.Property(t => t.ClosedAt).IsRequired(false);
        builder.Property(t => t.Notes).HasMaxLength(1000).IsRequired(false);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(t => new { t.ApplicationId, t.PatientId, t.Status });
        builder.HasIndex(t => new { t.ApplicationId, t.Status });

        builder.HasOne(t => t.Patient)
               .WithMany()
               .HasForeignKey(t => t.PatientId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
