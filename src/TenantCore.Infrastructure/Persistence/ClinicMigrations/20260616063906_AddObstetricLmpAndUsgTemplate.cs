using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddObstetricLmpAndUsgTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EddByLmp",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EddByUsg",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Lmp",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicUsgTemplates",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCustomized = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicUsgTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsgTemplateRows",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicUsgTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowOrder = table.Column<int>(type: "int", nullable: false),
                    WeekLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LmpDayOffset = table.Column<int>(type: "int", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Indication = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsgTemplateRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsgTemplateRows_ClinicUsgTemplates_ClinicUsgTemplateId",
                        column: x => x.ClinicUsgTemplateId,
                        principalSchema: "clinic",
                        principalTable: "ClinicUsgTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicUsgTemplates_ApplicationId",
                schema: "clinic",
                table: "ClinicUsgTemplates",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsgTemplateRows_ClinicUsgTemplateId",
                schema: "clinic",
                table: "UsgTemplateRows",
                column: "ClinicUsgTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsgTemplateRows",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ClinicUsgTemplates",
                schema: "clinic");

            migrationBuilder.DropColumn(
                name: "EddByLmp",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.DropColumn(
                name: "EddByUsg",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.DropColumn(
                name: "Lmp",
                schema: "clinic",
                table: "ObstetricPrescriptionData");
        }
    }
}
