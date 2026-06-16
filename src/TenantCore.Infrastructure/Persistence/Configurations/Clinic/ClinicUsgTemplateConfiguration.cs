using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicUsgTemplateConfiguration : IEntityTypeConfiguration<ClinicUsgTemplate>
{
    public void Configure(EntityTypeBuilder<ClinicUsgTemplate> builder)
    {
        builder.ToTable("ClinicUsgTemplates", "clinic");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.ApplicationId).IsRequired();
        builder.HasIndex(t => t.ApplicationId).IsUnique();

        builder.Property(t => t.IsCustomized).IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasMany(t => t.Rows)
               .WithOne()
               .HasForeignKey(r => r.ClinicUsgTemplateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
