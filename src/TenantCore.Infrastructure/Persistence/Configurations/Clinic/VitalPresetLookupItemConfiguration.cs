using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Persistence.Configurations.Clinic;

internal sealed class VitalPresetLookupItemConfiguration : IEntityTypeConfiguration<VitalPresetLookupItem>
{
    // Fixed seed timestamp — HasData snapshots values at migration-design time, so a
    // literal constant (not DateTime.UtcNow) keeps every future migration from re-diffing
    // these rows' CreatedAt on every unrelated migration.
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<VitalPresetLookupItem> builder)
    {
        builder.ToTable("VitalPresetLookupItems", "clinic");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.ApplicationId);
        builder.Property(v => v.VitalField).IsRequired();
        builder.Property(v => v.Value).IsRequired().HasMaxLength(64);

        builder.HasIndex(v => new { v.ApplicationId, v.VitalField, v.Value }).IsUnique();
        builder.HasIndex(v => v.VitalField);

        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasData(
            // BP
            Seed("a1b2c301-0001-0000-0000-000000000000", VitalFieldType.Bp, "110/70"),
            Seed("a1b2c301-0002-0000-0000-000000000000", VitalFieldType.Bp, "120/80"),
            Seed("a1b2c301-0003-0000-0000-000000000000", VitalFieldType.Bp, "130/85"),
            Seed("a1b2c301-0004-0000-0000-000000000000", VitalFieldType.Bp, "140/90"),
            Seed("a1b2c301-0005-0000-0000-000000000000", VitalFieldType.Bp, "150/95"),
            // Pulse
            Seed("a1b2c302-0001-0000-0000-000000000000", VitalFieldType.Pulse, "60"),
            Seed("a1b2c302-0002-0000-0000-000000000000", VitalFieldType.Pulse, "72"),
            Seed("a1b2c302-0003-0000-0000-000000000000", VitalFieldType.Pulse, "80"),
            Seed("a1b2c302-0004-0000-0000-000000000000", VitalFieldType.Pulse, "90"),
            Seed("a1b2c302-0005-0000-0000-000000000000", VitalFieldType.Pulse, "100"),
            // Temp
            Seed("a1b2c303-0001-0000-0000-000000000000", VitalFieldType.Temp, "97.5"),
            Seed("a1b2c303-0002-0000-0000-000000000000", VitalFieldType.Temp, "98.4"),
            Seed("a1b2c303-0003-0000-0000-000000000000", VitalFieldType.Temp, "98.6"),
            Seed("a1b2c303-0004-0000-0000-000000000000", VitalFieldType.Temp, "99.5"),
            Seed("a1b2c303-0005-0000-0000-000000000000", VitalFieldType.Temp, "100.4"),
            // Weight
            Seed("a1b2c304-0001-0000-0000-000000000000", VitalFieldType.Weight, "50"),
            Seed("a1b2c304-0002-0000-0000-000000000000", VitalFieldType.Weight, "60"),
            Seed("a1b2c304-0003-0000-0000-000000000000", VitalFieldType.Weight, "70"),
            Seed("a1b2c304-0004-0000-0000-000000000000", VitalFieldType.Weight, "80"),
            Seed("a1b2c304-0005-0000-0000-000000000000", VitalFieldType.Weight, "90"),
            // SpO2
            Seed("a1b2c305-0001-0000-0000-000000000000", VitalFieldType.SpO2, "94"),
            Seed("a1b2c305-0002-0000-0000-000000000000", VitalFieldType.SpO2, "96"),
            Seed("a1b2c305-0003-0000-0000-000000000000", VitalFieldType.SpO2, "97"),
            Seed("a1b2c305-0004-0000-0000-000000000000", VitalFieldType.SpO2, "98"),
            Seed("a1b2c305-0005-0000-0000-000000000000", VitalFieldType.SpO2, "99"),
            // RR
            Seed("a1b2c306-0001-0000-0000-000000000000", VitalFieldType.Rr, "12"),
            Seed("a1b2c306-0002-0000-0000-000000000000", VitalFieldType.Rr, "14"),
            Seed("a1b2c306-0003-0000-0000-000000000000", VitalFieldType.Rr, "16"),
            Seed("a1b2c306-0004-0000-0000-000000000000", VitalFieldType.Rr, "18"),
            Seed("a1b2c306-0005-0000-0000-000000000000", VitalFieldType.Rr, "20"),
            // Sugar
            Seed("a1b2c307-0001-0000-0000-000000000000", VitalFieldType.Sugar, "80"),
            Seed("a1b2c307-0002-0000-0000-000000000000", VitalFieldType.Sugar, "90"),
            Seed("a1b2c307-0003-0000-0000-000000000000", VitalFieldType.Sugar, "100"),
            Seed("a1b2c307-0004-0000-0000-000000000000", VitalFieldType.Sugar, "110"),
            Seed("a1b2c307-0005-0000-0000-000000000000", VitalFieldType.Sugar, "126")
        );
    }

    private static VitalPresetLookupItem Seed(string id, VitalFieldType vitalField, string value) =>
        VitalPresetLookupItem.CreateForSeed(Guid.Parse(id), vitalField, value, SeedCreatedAt);
}
