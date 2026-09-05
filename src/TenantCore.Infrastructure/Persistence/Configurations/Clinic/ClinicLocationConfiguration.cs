using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicLocationConfiguration : IEntityTypeConfiguration<ClinicLocation>
{
    public void Configure(EntityTypeBuilder<ClinicLocation> builder)
    {
        builder.ToTable("ClinicLocations", "clinic");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.ApplicationId).IsRequired();
        builder.HasIndex(l => l.ApplicationId).IsUnique();

        builder.Property(l => l.StateId).IsRequired();
        builder.Property(l => l.CityId).IsRequired();

        builder.Property(l => l.CreatedAt).IsRequired();

        builder.HasOne(l => l.State).WithMany().HasForeignKey(l => l.StateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.City).WithMany().HasForeignKey(l => l.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}
