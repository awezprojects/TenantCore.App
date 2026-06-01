using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("Beds", "clinic");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ApplicationId).IsRequired();
        builder.Property(b => b.WardId).IsRequired();
        builder.Property(b => b.RoomId).IsRequired();
        builder.HasIndex(b => b.RoomId);

        builder.Property(b => b.BedNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(b => new { b.RoomId, b.BedNumber }).IsUnique();

        builder.Property(b => b.IsOccupied).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.CreatedBy).HasMaxLength(256);
        builder.Property(b => b.UpdatedBy).HasMaxLength(256);
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasOne(b => b.Ward)
               .WithMany()
               .HasForeignKey(b => b.WardId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
