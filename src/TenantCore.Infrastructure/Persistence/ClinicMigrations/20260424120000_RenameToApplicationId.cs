using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class RenameToApplicationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_TenantId",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_TenantId_PhoneNumber",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_OpdRegistrations_TenantId",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_OpdRegistrations_TenantId_RegistrationNumber",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_IpdRegistrations_TenantId",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_IpdRegistrations_TenantId_AdmissionNumber",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClinicFeeConfigs_TenantId",
                schema: "clinic",
                table: "ClinicFeeConfigs");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "clinic",
                table: "Patients",
                newName: "ApplicationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "clinic",
                table: "OpdRegistrations",
                newName: "ApplicationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "clinic",
                table: "IpdRegistrations",
                newName: "ApplicationId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "clinic",
                table: "ClinicFeeConfigs",
                newName: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ApplicationId",
                schema: "clinic",
                table: "Patients",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ApplicationId_PhoneNumber",
                schema: "clinic",
                table: "Patients",
                columns: new[] { "ApplicationId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OpdRegistrations_ApplicationId",
                schema: "clinic",
                table: "OpdRegistrations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpdRegistrations_ApplicationId_RegistrationNumber",
                schema: "clinic",
                table: "OpdRegistrations",
                columns: new[] { "ApplicationId", "RegistrationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpdRegistrations_ApplicationId",
                schema: "clinic",
                table: "IpdRegistrations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdRegistrations_ApplicationId_AdmissionNumber",
                schema: "clinic",
                table: "IpdRegistrations",
                columns: new[] { "ApplicationId", "AdmissionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeeConfigs_ApplicationId",
                schema: "clinic",
                table: "ClinicFeeConfigs",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_ApplicationId",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_ApplicationId_PhoneNumber",
                schema: "clinic",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_OpdRegistrations_ApplicationId",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_OpdRegistrations_ApplicationId_RegistrationNumber",
                schema: "clinic",
                table: "OpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_IpdRegistrations_ApplicationId",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_IpdRegistrations_ApplicationId_AdmissionNumber",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClinicFeeConfigs_ApplicationId",
                schema: "clinic",
                table: "ClinicFeeConfigs");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                schema: "clinic",
                table: "Patients",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                schema: "clinic",
                table: "OpdRegistrations",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                schema: "clinic",
                table: "IpdRegistrations",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                schema: "clinic",
                table: "ClinicFeeConfigs",
                newName: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_TenantId",
                schema: "clinic",
                table: "Patients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_TenantId_PhoneNumber",
                schema: "clinic",
                table: "Patients",
                columns: new[] { "TenantId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OpdRegistrations_TenantId",
                schema: "clinic",
                table: "OpdRegistrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OpdRegistrations_TenantId_RegistrationNumber",
                schema: "clinic",
                table: "OpdRegistrations",
                columns: new[] { "TenantId", "RegistrationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpdRegistrations_TenantId",
                schema: "clinic",
                table: "IpdRegistrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdRegistrations_TenantId_AdmissionNumber",
                schema: "clinic",
                table: "IpdRegistrations",
                columns: new[] { "TenantId", "AdmissionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeeConfigs_TenantId",
                schema: "clinic",
                table: "ClinicFeeConfigs",
                column: "TenantId",
                unique: true);
        }
    }
}
