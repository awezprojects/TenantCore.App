using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddPatientMedicalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnownAllergies",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistory",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodGroup",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "KnownAllergies",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicalHistory",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                schema: "clinic",
                table: "Patients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
