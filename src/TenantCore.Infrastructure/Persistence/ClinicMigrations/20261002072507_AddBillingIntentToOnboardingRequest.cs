using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddBillingIntentToOnboardingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalBillingMode",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalFreeTestDays",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalInitialWalletCredit",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalIpdAdmissionRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalIpdDischargeRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalOpdRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalOtSurgeryRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalRegistrationFee",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 992, DateTimeKind.Utc).AddTicks(2898));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 992, DateTimeKind.Utc).AddTicks(2903));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 992, DateTimeKind.Utc).AddTicks(2907));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 992, DateTimeKind.Utc).AddTicks(2935));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 995, DateTimeKind.Utc).AddTicks(461));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 995, DateTimeKind.Utc).AddTicks(469));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 995, DateTimeKind.Utc).AddTicks(473));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 2, 7, 25, 3, 995, DateTimeKind.Utc).AddTicks(477));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalBillingMode",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalFreeTestDays",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalInitialWalletCredit",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalIpdAdmissionRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalIpdDischargeRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalOpdRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalOtSurgeryRate",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalRegistrationFee",
                schema: "clinic",
                table: "ClinicOnboardingRequests");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 68, DateTimeKind.Utc).AddTicks(1260));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 68, DateTimeKind.Utc).AddTicks(1265));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 68, DateTimeKind.Utc).AddTicks(1268));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 68, DateTimeKind.Utc).AddTicks(1280));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6427));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6434));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6454));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6458));
        }
    }
}
