using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionDesignFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                schema: "clinic",
                table: "Prescriptions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Investigations",
                schema: "clinic",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VitalBP",
                schema: "clinic",
                table: "Prescriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalPulse",
                schema: "clinic",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VitalRR",
                schema: "clinic",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitalSpO2",
                schema: "clinic",
                table: "Prescriptions",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitalSugar",
                schema: "clinic",
                table: "Prescriptions",
                type: "decimal(6,1)",
                precision: 6,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitalTemp",
                schema: "clinic",
                table: "Prescriptions",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitalWeight",
                schema: "clinic",
                table: "Prescriptions",
                type: "decimal(5,1)",
                precision: 5,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                schema: "clinic",
                table: "PrescriptionItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                schema: "clinic",
                table: "PrescriptionItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timing",
                schema: "clinic",
                table: "PrescriptionItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Diagnosis",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Investigations",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalBP",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalPulse",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalRR",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalSpO2",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalSugar",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalTemp",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VitalWeight",
                schema: "clinic",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Frequency",
                schema: "clinic",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "Instructions",
                schema: "clinic",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "Timing",
                schema: "clinic",
                table: "PrescriptionItems");
        }
    }
}
