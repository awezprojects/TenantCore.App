using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class HistoryLookupItemConfiguration : IEntityTypeConfiguration<HistoryLookupItem>
{
    public void Configure(EntityTypeBuilder<HistoryLookupItem> builder)
    {
        builder.ToTable("HistoryLookupItems", "clinic");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.ApplicationId);
        builder.Property(h => h.Type).IsRequired();
        builder.Property(h => h.Value).IsRequired().HasMaxLength(256);

        builder.HasIndex(h => new { h.ApplicationId, h.Type, h.Value }).IsUnique();
        builder.HasIndex(h => h.Type);

        builder.Property(h => h.CreatedAt).IsRequired();
    }
}
