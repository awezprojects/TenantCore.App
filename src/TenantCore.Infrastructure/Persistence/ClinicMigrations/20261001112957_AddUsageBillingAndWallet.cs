using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddUsageBillingAndWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FreeTestDays",
                schema: "clinic",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                schema: "clinic",
                table: "SubscriptionPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionPlanId",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "PlanCode",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BillingMode",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "FreeTestDays",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingBillingMode",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingBillingModeEffectiveFrom",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationFeeChargedAt",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationFeeChargedBy",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationFeeWaiveReason",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RegistrationFeeWaived",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationFeeWaivedByAdminEmail",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialStartedAt",
                schema: "clinic",
                table: "ClinicAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicUsageRates",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingItem = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicUsageRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicUsageRecords",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingItem = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayerMode = table.Column<int>(type: "int", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    WalletTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChargedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicWallets",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LowBalanceThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LastTopUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDebitAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicWalletTransactions",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicWalletTransactions", x => x.Id);
                });

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
                columns: new[] { "CreatedAt", "FreeTestDays" },
                values: new object[] { new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6427), null });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "FreeTestDays" },
                values: new object[] { new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6434), null });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "FreeTestDays" },
                values: new object[] { new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6454), null });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "FreeTestDays" },
                values: new object[] { new DateTime(2026, 10, 1, 11, 29, 56, 70, DateTimeKind.Utc).AddTicks(6458), null });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicUsageRates_ApplicationId_BillingItem",
                schema: "clinic",
                table: "ClinicUsageRates",
                columns: new[] { "ApplicationId", "BillingItem" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicUsageRecords_ApplicationId_BillingItem_ChargedAt",
                schema: "clinic",
                table: "ClinicUsageRecords",
                columns: new[] { "ApplicationId", "BillingItem", "ChargedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicUsageRecords_ApplicationId_BillingItem_SourceEntityId",
                schema: "clinic",
                table: "ClinicUsageRecords",
                columns: new[] { "ApplicationId", "BillingItem", "SourceEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicUsageRecords_ApplicationId_ChargedAt",
                schema: "clinic",
                table: "ClinicUsageRecords",
                columns: new[] { "ApplicationId", "ChargedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicWallets_ApplicationId",
                schema: "clinic",
                table: "ClinicWallets",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicWalletTransactions_ApplicationId_OccurredAt",
                schema: "clinic",
                table: "ClinicWalletTransactions",
                columns: new[] { "ApplicationId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicWalletTransactions_ApplicationId_ReferenceType_ReferenceId",
                schema: "clinic",
                table: "ClinicWalletTransactions",
                columns: new[] { "ApplicationId", "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "[ReferenceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicUsageRates",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ClinicUsageRecords",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ClinicWallets",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ClinicWalletTransactions",
                schema: "clinic");

            migrationBuilder.DropColumn(
                name: "FreeTestDays",
                schema: "clinic",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                schema: "clinic",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "BillingMode",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "FreeTestDays",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "PendingBillingMode",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "PendingBillingModeEffectiveFrom",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFeeChargedAt",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFeeChargedBy",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFeeWaiveReason",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFeeWaived",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationFeeWaivedByAdminEmail",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.DropColumn(
                name: "TrialStartedAt",
                schema: "clinic",
                table: "ClinicAccounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionPlanId",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlanCode",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 823, DateTimeKind.Utc).AddTicks(7417));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 823, DateTimeKind.Utc).AddTicks(7431));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 823, DateTimeKind.Utc).AddTicks(7434));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 823, DateTimeKind.Utc).AddTicks(7436));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 825, DateTimeKind.Utc).AddTicks(9337));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 825, DateTimeKind.Utc).AddTicks(9357));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 825, DateTimeKind.Utc).AddTicks(9360));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 14, 43, 19, 825, DateTimeKind.Utc).AddTicks(9362));
        }
    }
}
