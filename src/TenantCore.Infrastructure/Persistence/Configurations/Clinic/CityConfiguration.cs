using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.StateId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => new { c.StateId, c.Name }).IsUnique();

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne(c => c.State)
               .WithMany(s => s.Cities)
               .HasForeignKey(c => c.StateId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
