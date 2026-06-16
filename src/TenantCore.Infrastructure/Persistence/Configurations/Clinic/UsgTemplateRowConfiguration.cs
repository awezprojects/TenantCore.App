using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class UsgTemplateRowConfiguration : IEntityTypeConfiguration<UsgTemplateRow>
{
    public void Configure(EntityTypeBuilder<UsgTemplateRow> builder)
    {
        builder.ToTable("UsgTemplateRows", "clinic");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ClinicUsgTemplateId).IsRequired();
        builder.Property(r => r.RowOrder).IsRequired();
        builder.Property(r => r.WeekLabel).IsRequired().HasMaxLength(50);
        builder.Property(r => r.LmpDayOffset).IsRequired();
        builder.Property(r => r.Activity).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Indication).IsRequired().HasMaxLength(500);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
