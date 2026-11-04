using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class OpdRegistrationNumberCounterConfiguration : IEntityTypeConfiguration<OpdRegistrationNumberCounter>
{
    public void Configure(EntityTypeBuilder<OpdRegistrationNumberCounter> builder)
    {
        builder.ToTable("OpdRegistrationNumberCounters", "clinic");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ApplicationId).IsRequired();
        builder.Property(c => c.CounterDate).IsRequired().HasColumnType("date");
        builder.Property(c => c.LastSequence).IsRequired();

        // One counter per clinic per day — GetNextSequenceAsync reads this by (ApplicationId, CounterDate).
        builder.HasIndex(c => new { c.ApplicationId, c.CounterDate }).IsUnique();

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
