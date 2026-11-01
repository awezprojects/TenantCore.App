using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class ClinicPlanOfferConfiguration : IEntityTypeConfiguration<ClinicPlanOffer>
{
    public void Configure(EntityTypeBuilder<ClinicPlanOffer> builder)
    {
        builder.ToTable("ClinicPlanOffers", "clinic", t =>
        {
            t.HasCheckConstraint("CK_ClinicPlanOffers_OfferPrice_NonNegative", "[OfferPrice] IS NULL OR [OfferPrice] >= 0");
        });
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.ApplicationId).IsRequired();
        builder.Property(o => o.SubscriptionPlanId).IsRequired();
        builder.Property(o => o.OfferPrice).HasPrecision(18, 2);
        builder.Property(o => o.ValidUntil);
        builder.Property(o => o.Note).HasMaxLength(250);
        builder.Property(o => o.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(o => o.CreatedByAdminEmail).IsRequired().HasMaxLength(256);
        builder.Property(o => o.WithdrawnByAdminEmail).HasMaxLength(256);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(256);
        builder.Property(o => o.UpdatedBy).HasMaxLength(256);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => new { o.ApplicationId, o.IsActive });

        // At most one LIVE offer per (clinic, plan). Withdrawn rows stay for history, so the
        // uniqueness is filtered on IsActive rather than covering the whole table.
        builder.HasIndex(o => new { o.ApplicationId, o.SubscriptionPlanId })
               .IsUnique()
               .HasFilter("[IsActive] = 1");

        builder.HasOne(o => o.Plan)
               .WithMany()
               .HasForeignKey(o => o.SubscriptionPlanId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
