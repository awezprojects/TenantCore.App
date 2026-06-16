using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionPrintSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideClinicHeader",
                schema: "clinic",
                table: "PrescriptionConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrintMarginBottom",
                schema: "clinic",
                table: "PrescriptionConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrintMarginLeft",
                schema: "clinic",
                table: "PrescriptionConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrintMarginRight",
                schema: "clinic",
                table: "PrescriptionConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrintMarginTop",
                schema: "clinic",
                table: "PrescriptionConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideClinicHeader",
                schema: "clinic",
                table: "PrescriptionConfigs");

            migrationBuilder.DropColumn(
                name: "PrintMarginBottom",
                schema: "clinic",
                table: "PrescriptionConfigs");

            migrationBuilder.DropColumn(
                name: "PrintMarginLeft",
                schema: "clinic",
                table: "PrescriptionConfigs");

            migrationBuilder.DropColumn(
                name: "PrintMarginRight",
                schema: "clinic",
                table: "PrescriptionConfigs");

            migrationBuilder.DropColumn(
                name: "PrintMarginTop",
                schema: "clinic",
                table: "PrescriptionConfigs");
        }
    }
}
