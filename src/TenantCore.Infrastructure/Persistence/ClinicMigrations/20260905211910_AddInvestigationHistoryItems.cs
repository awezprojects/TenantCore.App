using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddInvestigationHistoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedGlobalDefaults(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 22, DateTimeKind.Utc).AddTicks(8267));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 22, DateTimeKind.Utc).AddTicks(8272));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 22, DateTimeKind.Utc).AddTicks(8274));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 22, DateTimeKind.Utc).AddTicks(8276));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 23, DateTimeKind.Utc).AddTicks(5753));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 23, DateTimeKind.Utc).AddTicks(5762));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 23, DateTimeKind.Utc).AddTicks(5776));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 21, 19, 9, 23, DateTimeKind.Utc).AddTicks(5778));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [clinic].[HistoryLookupItems] WHERE [Type] = 9 AND [ApplicationId] IS NULL");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 763, DateTimeKind.Utc).AddTicks(4590));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 763, DateTimeKind.Utc).AddTicks(4612));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 763, DateTimeKind.Utc).AddTicks(4616));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 763, DateTimeKind.Utc).AddTicks(4639));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 767, DateTimeKind.Utc).AddTicks(1702));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 767, DateTimeKind.Utc).AddTicks(1715));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 767, DateTimeKind.Utc).AddTicks(1735));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 19, 15, 27, 767, DateTimeKind.Utc).AddTicks(1755));
        }

        // Seeds the same investigations already offered by the categorized "+ Add from list"
        // picker (PrescriptionForm.razor's _investigationsMaster) into the shared history-lookup
        // table under the new Investigation type, so the new search-and-add box finds the same
        // tests plus whatever clinics/doctors add later — one unified, growing list.
        private static void SeedGlobalDefaults(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            SeedType(migrationBuilder, "e9000000", 9, seedTime,
            [
                // Blood Tests
                "CBC",
                "ESR",
                "Blood Sugar (Fasting)",
                "Blood Sugar (PP)",
                "HbA1c",
                "Lipid Profile",
                "LFT",
                "KFT",
                "Thyroid Profile (TSH, T3, T4)",
                "Vitamin D3",
                "Vitamin B12",
                "Iron Studies",
                "CRP",
                "Uric Acid",
                // Urine
                "Urine Routine",
                "Urine Culture & Sensitivity",
                "Urine Microalbumin",
                // Cardiac
                "ECG",
                "2D Echo",
                "TMT (Stress Test)",
                "Holter Monitoring",
                "Troponin-I",
                // Radiology
                "Chest X-Ray",
                "Abdominal Ultrasound",
                "CT Brain",
                "CT Chest",
                "MRI Brain",
                "X-Ray Spine",
                // Other
                "EEG",
                "Pulmonary Function Test",
                "Endoscopy",
                "Colonoscopy",
                "Pap Smear",
            ]);
        }

        private static void SeedType(
            MigrationBuilder migrationBuilder, string guidPrefix, int type, DateTime seedTime, string[] values)
        {
            var rows = new object[values.Length, 4];
            for (var i = 0; i < values.Length; i++)
            {
                rows[i, 0] = Guid.Parse($"{guidPrefix}-0000-0000-0000-{i + 1:D12}");
                rows[i, 1] = type;
                rows[i, 2] = values[i];
                rows[i, 3] = seedTime;
            }

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "HistoryLookupItems",
                columns: new[] { "Id", "Type", "Value", "CreatedAt" },
                values: rows);
        }
    }
}
