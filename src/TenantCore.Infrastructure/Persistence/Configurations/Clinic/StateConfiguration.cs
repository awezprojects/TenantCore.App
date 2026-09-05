using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("States", "clinic");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(10);
        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.Code).IsUnique();

        builder.Property(s => s.CreatedAt).IsRequired();
    }
}
