using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddOpdVitals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodPressure",
                schema: "clinic",
                table: "OpdRegistrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OxygenSaturation",
                schema: "clinic",
                table: "OpdRegistrations",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PulseRate",
                schema: "clinic",
                table: "OpdRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                schema: "clinic",
                table: "OpdRegistrations",
                type: "decimal(5,1)",
                precision: 5,
                scale: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodPressure",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropColumn(
                name: "OxygenSaturation",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropColumn(
                name: "PulseRate",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropColumn(
                name: "Weight",
                schema: "clinic",
                table: "OpdRegistrations");
        }
    }
}
