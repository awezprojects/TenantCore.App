using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ParticularConfiguration : IEntityTypeConfiguration<Particular>
{
    public void Configure(EntityTypeBuilder<Particular> builder)
    {
        builder.ToTable("Particulars", "clinic");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ApplicationId).IsRequired();
        builder.HasIndex(p => p.ApplicationId);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.DefaultAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.IsActive).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
