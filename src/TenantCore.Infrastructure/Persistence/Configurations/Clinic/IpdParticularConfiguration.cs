using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class IpdParticularConfiguration : IEntityTypeConfiguration<IpdParticular>
{
    public void Configure(EntityTypeBuilder<IpdParticular> builder)
    {
        builder.ToTable("IpdParticulars", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ApplicationId).IsRequired();
        builder.Property(p => p.IpdRegistrationId).IsRequired();
        builder.HasIndex(p => new { p.ApplicationId, p.IpdRegistrationId });

        builder.Property(p => p.ParticularId).IsRequired();
        builder.Property(p => p.ParticularName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.AddedByUserId).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
