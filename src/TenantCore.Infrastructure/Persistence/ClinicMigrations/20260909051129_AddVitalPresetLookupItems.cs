using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddVitalPresetLookupItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VitalPresetLookupItems",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VitalField = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalPresetLookupItems", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 902, DateTimeKind.Utc).AddTicks(7761));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 902, DateTimeKind.Utc).AddTicks(7766));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 902, DateTimeKind.Utc).AddTicks(7770));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 902, DateTimeKind.Utc).AddTicks(7781));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 903, DateTimeKind.Utc).AddTicks(7873));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 903, DateTimeKind.Utc).AddTicks(7885));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 903, DateTimeKind.Utc).AddTicks(7904));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 5, 11, 26, 903, DateTimeKind.Utc).AddTicks(7908));

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "VitalPresetLookupItems",
                columns: new[] { "Id", "ApplicationId", "CreatedAt", "RowVersion", "UpdatedAt", "Value", "VitalField" },
                values: new object[,]
                {
                    { new Guid("a1b2c301-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "110/70", 1 },
                    { new Guid("a1b2c301-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "120/80", 1 },
                    { new Guid("a1b2c301-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "130/85", 1 },
                    { new Guid("a1b2c301-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "140/90", 1 },
                    { new Guid("a1b2c301-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "150/95", 1 },
                    { new Guid("a1b2c302-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "60", 2 },
                    { new Guid("a1b2c302-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "72", 2 },
                    { new Guid("a1b2c302-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "80", 2 },
                    { new Guid("a1b2c302-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "90", 2 },
                    { new Guid("a1b2c302-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "100", 2 },
                    { new Guid("a1b2c303-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "97.5", 3 },
                    { new Guid("a1b2c303-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "98.4", 3 },
                    { new Guid("a1b2c303-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "98.6", 3 },
                    { new Guid("a1b2c303-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "99.5", 3 },
                    { new Guid("a1b2c303-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "100.4", 3 },
                    { new Guid("a1b2c304-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "50", 4 },
                    { new Guid("a1b2c304-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "60", 4 },
                    { new Guid("a1b2c304-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "70", 4 },
                    { new Guid("a1b2c304-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "80", 4 },
                    { new Guid("a1b2c304-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "90", 4 },
                    { new Guid("a1b2c305-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "94", 5 },
                    { new Guid("a1b2c305-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "96", 5 },
                    { new Guid("a1b2c305-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "97", 5 },
                    { new Guid("a1b2c305-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "98", 5 },
                    { new Guid("a1b2c305-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "99", 5 },
                    { new Guid("a1b2c306-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "12", 6 },
                    { new Guid("a1b2c306-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "14", 6 },
                    { new Guid("a1b2c306-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "16", 6 },
                    { new Guid("a1b2c306-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "18", 6 },
                    { new Guid("a1b2c306-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "20", 6 },
                    { new Guid("a1b2c307-0001-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "80", 7 },
                    { new Guid("a1b2c307-0002-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "90", 7 },
                    { new Guid("a1b2c307-0003-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "100", 7 },
                    { new Guid("a1b2c307-0004-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "110", 7 },
                    { new Guid("a1b2c307-0005-0000-0000-000000000000"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "126", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VitalPresetLookupItems_ApplicationId_VitalField_Value",
                schema: "clinic",
                table: "VitalPresetLookupItems",
                columns: new[] { "ApplicationId", "VitalField", "Value" },
                unique: true,
                filter: "[ApplicationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VitalPresetLookupItems_VitalField",
                schema: "clinic",
                table: "VitalPresetLookupItems",
                column: "VitalField");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VitalPresetLookupItems",
                schema: "clinic");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 876, DateTimeKind.Utc).AddTicks(6358));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 876, DateTimeKind.Utc).AddTicks(6362));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 876, DateTimeKind.Utc).AddTicks(6368));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 876, DateTimeKind.Utc).AddTicks(6385));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 877, DateTimeKind.Utc).AddTicks(2543));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 877, DateTimeKind.Utc).AddTicks(2567));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 877, DateTimeKind.Utc).AddTicks(2580));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 8, 8, 10, 25, 877, DateTimeKind.Utc).AddTicks(2583));
        }
    }
}
