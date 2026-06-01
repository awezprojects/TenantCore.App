using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("Wards", "clinic");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.ApplicationId).IsRequired();
        builder.HasIndex(w => w.ApplicationId);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(w => new { w.ApplicationId, w.Name }).IsUnique();

        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.IsActive).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.CreatedBy).HasMaxLength(256);
        builder.Property(w => w.UpdatedBy).HasMaxLength(256);
        builder.Property(w => w.RowVersion).IsRowVersion();

        builder.HasMany(w => w.Rooms)
               .WithOne(r => r.Ward)
               .HasForeignKey(r => r.WardId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
