using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddMedicineDosageFormAndNormalizationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DosageFormId",
                table: "Medicines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DosageFormMappedAt",
                table: "Medicines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDosageFormMapped",
                table: "Medicines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MedicineDosageForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineDosageForms", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MedicineDosageForms",
                columns: new[] { "Id", "CreatedDate", "CreatedBy", "Description", "IsActive", "Name", "RowVersion", "ModifiedDate", "ModifiedBy" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Solid oral dosage form", true, "Tablet", null, null, null },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sustained-release tablet", true, "Tablet SR", null, null, null },
                    { new Guid("a1b2c3d4-0003-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Extended-release tablet", true, "Tablet XR", null, null, null },
                    { new Guid("a1b2c3d4-0004-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Hard or soft shell capsule", true, "Capsule", null, null, null },
                    { new Guid("a1b2c3d4-0005-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Liquid oral dosage form", true, "Syrup", null, null, null },
                    { new Guid("a1b2c3d4-0006-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Powder reconstituted as syrup", true, "Dry Syrup", null, null, null },
                    { new Guid("a1b2c3d4-0007-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Topical semi-solid emulsion", true, "Cream", null, null, null },
                    { new Guid("a1b2c3d4-0008-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ophthalmic liquid drops", true, "Eye Drop", null, null, null },
                    { new Guid("a1b2c3d4-0009-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oral or nasal liquid drops", true, "Drop", null, null, null },
                    { new Guid("a1b2c3d4-0010-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Parenteral dosage form", true, "Injection", null, null, null },
                    { new Guid("a1b2c3d4-0011-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Intravenous infusion solution", true, "Infusion", null, null, null },
                    { new Guid("a1b2c3d4-0012-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Inhalation device", true, "Inhaler", null, null, null },
                    { new Guid("a1b2c3d4-0013-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Transdermal patch", true, "Patch", null, null, null },
                    { new Guid("a1b2c3d4-0014-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dry powder formulation", true, "Powder", null, null, null },
                    { new Guid("a1b2c3d4-0015-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Topical gel", true, "Gel", null, null, null },
                    { new Guid("a1b2c3d4-0016-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Topical ointment", true, "Ointment", null, null, null },
                    { new Guid("a1b2c3d4-0017-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Topical lotion", true, "Lotion", null, null, null },
                    { new Guid("a1b2c3d4-0018-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Eye suspension", true, "Ophthalmic Suspension", null, null, null },
                    { new Guid("a1b2c3d4-0019-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oral liquid suspension", true, "Oral Suspension", null, null, null },
                    { new Guid("a1b2c3d4-0020-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Eye solution", true, "Ophthalmic Solution", null, null, null },
                    { new Guid("a1b2c3d4-0021-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Liquid solution", true, "Solution", null, null, null },
                    { new Guid("a1b2c3d4-0022-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nasal or topical spray", true, "Spray", null, null, null },
                    { new Guid("a1b2c3d4-0023-0000-0000-000000000000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oral liquid solution", true, "Oral Solution", null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_DosageFormId",
                table: "Medicines",
                column: "DosageFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_IsDosageFormMapped",
                table: "Medicines",
                column: "IsDosageFormMapped");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineDosageForms_Name",
                table: "MedicineDosageForms",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_MedicineDosageForms_DosageFormId",
                table: "Medicines",
                column: "DosageFormId",
                principalTable: "MedicineDosageForms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_MedicineDosageForms_DosageFormId",
                table: "Medicines");

            migrationBuilder.DropTable(
                name: "MedicineDosageForms");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_DosageFormId",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_IsDosageFormMapped",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "DosageFormId",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "DosageFormMappedAt",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "IsDosageFormMapped",
                table: "Medicines");
        }
    }
}
