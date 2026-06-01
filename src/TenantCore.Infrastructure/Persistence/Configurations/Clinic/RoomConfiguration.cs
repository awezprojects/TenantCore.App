using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", "clinic");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ApplicationId).IsRequired();
        builder.Property(r => r.WardId).IsRequired();
        builder.HasIndex(r => r.WardId);

        builder.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(r => new { r.WardId, r.RoomNumber }).IsUnique();

        builder.Property(r => r.RoomType).HasMaxLength(100);
        builder.Property(r => r.PricePerDay).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.UpdatedBy).HasMaxLength(256);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasMany(r => r.Beds)
               .WithOne(b => b.Room)
               .HasForeignKey(b => b.RoomId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
