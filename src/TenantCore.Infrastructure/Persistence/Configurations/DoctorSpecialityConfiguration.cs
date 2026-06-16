using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence.Configurations;

internal sealed class DoctorSpecialityConfiguration : IEntityTypeConfiguration<DoctorSpeciality>
{
    // Fixed GUIDs so seed data is idempotent across migrations
    private static readonly (Guid Id, string Name, string? Description, int Sort)[] Seeds =
    [
        (Guid.Parse("d0000001-0000-0000-0000-000000000001"), "General Medicine",          "Diagnosis and treatment of common illnesses",             1),
        (Guid.Parse("d0000001-0000-0000-0000-000000000002"), "General Surgery",            "Surgical treatment of abdominal and soft-tissue conditions",2),
        (Guid.Parse("d0000001-0000-0000-0000-000000000003"), "Internal Medicine",          "Non-surgical management of adult diseases",               3),
        (Guid.Parse("d0000001-0000-0000-0000-000000000004"), "Family Medicine",            "Comprehensive care for patients of all ages",             4),
        (Guid.Parse("d0000001-0000-0000-0000-000000000005"), "Orthopedics",               "Bone, joint, muscle and spine disorders",                 5),
        (Guid.Parse("d0000001-0000-0000-0000-000000000006"), "Pediatrics",                "Medical care for infants, children and adolescents",      6),
        (Guid.Parse("d0000001-0000-0000-0000-000000000007"), "Ophthalmology",             "Diseases and surgery of the eye",                        7),
        (Guid.Parse("d0000001-0000-0000-0000-000000000008"), "Cardiology",                "Heart and cardiovascular system disorders",               8),
        (Guid.Parse("d0000001-0000-0000-0000-000000000009"), "Dermatology",               "Skin, hair and nail conditions",                         9),
        (Guid.Parse("d0000001-0000-0000-0000-000000000010"), "Gynecology & Obstetrics",   "Women's reproductive health and pregnancy",               10),
        (Guid.Parse("d0000001-0000-0000-0000-000000000011"), "ENT",                       "Ear, nose and throat disorders",                         11),
        (Guid.Parse("d0000001-0000-0000-0000-000000000012"), "Neurology",                 "Brain, spinal cord and nervous system disorders",         12),
        (Guid.Parse("d0000001-0000-0000-0000-000000000013"), "Psychiatry",                "Mental, behavioural and emotional disorders",             13),
        (Guid.Parse("d0000001-0000-0000-0000-000000000014"), "Pulmonology",               "Lungs and respiratory system diseases",                   14),
        (Guid.Parse("d0000001-0000-0000-0000-000000000015"), "Gastroenterology",          "Digestive system and gastrointestinal disorders",         15),
        (Guid.Parse("d0000001-0000-0000-0000-000000000016"), "Nephrology",                "Kidney diseases and renal disorders",                     16),
        (Guid.Parse("d0000001-0000-0000-0000-000000000017"), "Urology",                   "Urinary tract and male reproductive system",              17),
        (Guid.Parse("d0000001-0000-0000-0000-000000000018"), "Oncology",                  "Cancer diagnosis and treatment",                          18),
        (Guid.Parse("d0000001-0000-0000-0000-000000000019"), "Endocrinology",             "Hormonal and metabolic disorders",                       19),
        (Guid.Parse("d0000001-0000-0000-0000-000000000020"), "Rheumatology",              "Arthritis and autoimmune musculoskeletal diseases",       20),
        (Guid.Parse("d0000001-0000-0000-0000-000000000021"), "Anesthesiology",            "Anesthesia and perioperative care",                       21),
        (Guid.Parse("d0000001-0000-0000-0000-000000000022"), "Radiology",                 "Medical imaging and diagnostics",                        22),
        (Guid.Parse("d0000001-0000-0000-0000-000000000023"), "Pathology",                 "Laboratory diagnosis of disease",                        23),
        (Guid.Parse("d0000001-0000-0000-0000-000000000024"), "Emergency Medicine",        "Acute illness and emergency care",                       24),
        (Guid.Parse("d0000001-0000-0000-0000-000000000025"), "Dentistry",                 "Oral health, teeth and gum diseases",                    25),
        (Guid.Parse("d0000001-0000-0000-0000-000000000026"), "Physiotherapy",             "Physical rehabilitation and movement disorders",          26),
        (Guid.Parse("d0000001-0000-0000-0000-000000000027"), "Nutrition & Dietetics",     "Diet, nutrition and metabolic health",                   27),
        (Guid.Parse("d0000001-0000-0000-0000-000000000028"), "Neurosurgery",              "Surgical treatment of neurological conditions",           28),
        (Guid.Parse("d0000001-0000-0000-0000-000000000029"), "Plastic Surgery",           "Reconstructive and cosmetic surgical procedures",         29),
        (Guid.Parse("d0000001-0000-0000-0000-000000000030"), "Other",                     null,                                                      99),
    ];

    private static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<DoctorSpeciality> builder)
    {
        builder.ToTable("DoctorSpecialities", "clinic");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(300);
        builder.Property(s => s.SortOrder).HasDefaultValue(0);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(s => s.DoctorProfiles)
               .WithOne(dp => dp.Speciality)
               .HasForeignKey(dp => dp.SpecialityId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasData(Seeds.Select(s => new
        {
            Id          = s.Id,
            Name        = s.Name,
            Description = s.Description,
            SortOrder   = s.Sort,
            IsActive    = true,
            CreatedAt   = SeedDate,
            UpdatedAt   = (DateTime?)null,
            RowVersion  = (byte[]?)null,
        }));
    }
}
