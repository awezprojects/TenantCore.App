using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddIpdShiftChargingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentType",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManualChargeAt",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManualChargeByUserId",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualChargeReason",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IpdFullDayShiftRateRule",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "IpdManualShiftChargeMode",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "IpdShiftGraceRule",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IpdShiftGraceWindowMinutes",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 161, DateTimeKind.Utc).AddTicks(6372));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 161, DateTimeKind.Utc).AddTicks(6377));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 161, DateTimeKind.Utc).AddTicks(6380));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 161, DateTimeKind.Utc).AddTicks(6382));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 163, DateTimeKind.Utc).AddTicks(9717));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 163, DateTimeKind.Utc).AddTicks(9728));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 163, DateTimeKind.Utc).AddTicks(9733));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 13, 14, 52, 28, 163, DateTimeKind.Utc).AddTicks(9761));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentType",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "AppliedChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "ManualChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "ManualChargeAt",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "ManualChargeByUserId",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "ManualChargeReason",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "StandardChargeAmount",
                schema: "clinic",
                table: "IpdBedTransferHistories");

            migrationBuilder.DropColumn(
                name: "IpdFullDayShiftRateRule",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.DropColumn(
                name: "IpdManualShiftChargeMode",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.DropColumn(
                name: "IpdShiftGraceRule",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.DropColumn(
                name: "IpdShiftGraceWindowMinutes",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 732, DateTimeKind.Utc).AddTicks(9020));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 732, DateTimeKind.Utc).AddTicks(9025));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 732, DateTimeKind.Utc).AddTicks(9028));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 732, DateTimeKind.Utc).AddTicks(9041));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 736, DateTimeKind.Utc).AddTicks(162));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 736, DateTimeKind.Utc).AddTicks(195));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 736, DateTimeKind.Utc).AddTicks(233));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 12, 9, 40, 37, 736, DateTimeKind.Utc).AddTicks(236));
        }
    }
}
