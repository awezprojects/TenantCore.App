using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddIpdCancelWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IpdCancelWindowMinutes",
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
                value: new DateTime(2026, 11, 15, 14, 41, 50, 806, DateTimeKind.Utc).AddTicks(1359));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 806, DateTimeKind.Utc).AddTicks(1367));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 806, DateTimeKind.Utc).AddTicks(1370));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 806, DateTimeKind.Utc).AddTicks(1390));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 809, DateTimeKind.Utc).AddTicks(6758));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 809, DateTimeKind.Utc).AddTicks(6855));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 809, DateTimeKind.Utc).AddTicks(6894));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 15, 14, 41, 50, 809, DateTimeKind.Utc).AddTicks(6898));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpdCancelWindowMinutes",
                schema: "clinic",
                table: "ClinicFeatureFlags");

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
    }
}
