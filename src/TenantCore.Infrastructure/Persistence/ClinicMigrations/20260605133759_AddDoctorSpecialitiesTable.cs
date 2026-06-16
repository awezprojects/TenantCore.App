using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddDoctorSpecialitiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SpecialityId",
                table: "DoctorProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorSpecialities",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSpecialities", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "DoctorSpecialities",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "RowVersion", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("d0000001-0000-0000-0000-000000000001"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diagnosis and treatment of common illnesses", true, "General Medicine", null, 1, null },
                    { new Guid("d0000001-0000-0000-0000-000000000002"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Surgical treatment of abdominal and soft-tissue conditions", true, "General Surgery", null, 2, null },
                    { new Guid("d0000001-0000-0000-0000-000000000003"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Non-surgical management of adult diseases", true, "Internal Medicine", null, 3, null },
                    { new Guid("d0000001-0000-0000-0000-000000000004"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comprehensive care for patients of all ages", true, "Family Medicine", null, 4, null },
                    { new Guid("d0000001-0000-0000-0000-000000000005"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bone, joint, muscle and spine disorders", true, "Orthopedics", null, 5, null },
                    { new Guid("d0000001-0000-0000-0000-000000000006"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Medical care for infants, children and adolescents", true, "Pediatrics", null, 6, null },
                    { new Guid("d0000001-0000-0000-0000-000000000007"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diseases and surgery of the eye", true, "Ophthalmology", null, 7, null },
                    { new Guid("d0000001-0000-0000-0000-000000000008"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Heart and cardiovascular system disorders", true, "Cardiology", null, 8, null },
                    { new Guid("d0000001-0000-0000-0000-000000000009"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Skin, hair and nail conditions", true, "Dermatology", null, 9, null },
                    { new Guid("d0000001-0000-0000-0000-000000000010"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Women's reproductive health and pregnancy", true, "Gynecology & Obstetrics", null, 10, null },
                    { new Guid("d0000001-0000-0000-0000-000000000011"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ear, nose and throat disorders", true, "ENT", null, 11, null },
                    { new Guid("d0000001-0000-0000-0000-000000000012"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Brain, spinal cord and nervous system disorders", true, "Neurology", null, 12, null },
                    { new Guid("d0000001-0000-0000-0000-000000000013"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mental, behavioural and emotional disorders", true, "Psychiatry", null, 13, null },
                    { new Guid("d0000001-0000-0000-0000-000000000014"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lungs and respiratory system diseases", true, "Pulmonology", null, 14, null },
                    { new Guid("d0000001-0000-0000-0000-000000000015"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Digestive system and gastrointestinal disorders", true, "Gastroenterology", null, 15, null },
                    { new Guid("d0000001-0000-0000-0000-000000000016"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kidney diseases and renal disorders", true, "Nephrology", null, 16, null },
                    { new Guid("d0000001-0000-0000-0000-000000000017"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Urinary tract and male reproductive system", true, "Urology", null, 17, null },
                    { new Guid("d0000001-0000-0000-0000-000000000018"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cancer diagnosis and treatment", true, "Oncology", null, 18, null },
                    { new Guid("d0000001-0000-0000-0000-000000000019"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hormonal and metabolic disorders", true, "Endocrinology", null, 19, null },
                    { new Guid("d0000001-0000-0000-0000-000000000020"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arthritis and autoimmune musculoskeletal diseases", true, "Rheumatology", null, 20, null },
                    { new Guid("d0000001-0000-0000-0000-000000000021"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Anesthesia and perioperative care", true, "Anesthesiology", null, 21, null },
                    { new Guid("d0000001-0000-0000-0000-000000000022"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Medical imaging and diagnostics", true, "Radiology", null, 22, null },
                    { new Guid("d0000001-0000-0000-0000-000000000023"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laboratory diagnosis of disease", true, "Pathology", null, 23, null },
                    { new Guid("d0000001-0000-0000-0000-000000000024"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acute illness and emergency care", true, "Emergency Medicine", null, 24, null },
                    { new Guid("d0000001-0000-0000-0000-000000000025"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oral health, teeth and gum diseases", true, "Dentistry", null, 25, null },
                    { new Guid("d0000001-0000-0000-0000-000000000026"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Physical rehabilitation and movement disorders", true, "Physiotherapy", null, 26, null },
                    { new Guid("d0000001-0000-0000-0000-000000000027"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Diet, nutrition and metabolic health", true, "Nutrition & Dietetics", null, 27, null },
                    { new Guid("d0000001-0000-0000-0000-000000000028"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Surgical treatment of neurological conditions", true, "Neurosurgery", null, 28, null },
                    { new Guid("d0000001-0000-0000-0000-000000000029"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reconstructive and cosmetic surgical procedures", true, "Plastic Surgery", null, 29, null },
                    { new Guid("d0000001-0000-0000-0000-000000000030"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Other", null, 99, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_SpecialityId",
                table: "DoctorProfiles",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSpecialities_Name",
                schema: "clinic",
                table: "DoctorSpecialities",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorProfiles_DoctorSpecialities_SpecialityId",
                table: "DoctorProfiles",
                column: "SpecialityId",
                principalSchema: "clinic",
                principalTable: "DoctorSpecialities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorProfiles_DoctorSpecialities_SpecialityId",
                table: "DoctorProfiles");

            migrationBuilder.DropTable(
                name: "DoctorSpecialities",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_SpecialityId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "SpecialityId",
                table: "DoctorProfiles");
        }
    }
}
