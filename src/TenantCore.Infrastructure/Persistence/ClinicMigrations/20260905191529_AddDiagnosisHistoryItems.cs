using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisHistoryItems : Migration
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [clinic].[HistoryLookupItems] WHERE [Type] = 8 AND [ApplicationId] IS NULL");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8728));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8735));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8739));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8777));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5160));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5175));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5199));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5203));
        }

        // 60 commonly-used diagnosis / clinical-impression entries for the new Diagnosis
        // history type — global (ApplicationId = null), so every clinic can use them out
        // of the box, same as the other history chip fields.
        private static void SeedGlobalDefaults(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            SeedType(migrationBuilder, "e8000000", 8, seedTime,
            [
                "Hypertension",
                "Type 2 Diabetes Mellitus",
                "Type 1 Diabetes Mellitus",
                "Coronary Artery Disease (CAD)",
                "GERD (Gastroesophageal Reflux Disease)",
                "Upper Respiratory Tract Infection (URTI)",
                "Lower Respiratory Tract Infection (LRTI)",
                "Anaemia",
                "Iron Deficiency Anaemia",
                "Hypothyroidism",
                "Hyperthyroidism",
                "Bronchial Asthma",
                "Chronic Kidney Disease (CKD)",
                "Acute Gastroenteritis",
                "Urinary Tract Infection (UTI)",
                "Viral Fever",
                "Dengue Fever",
                "Typhoid Fever",
                "Malaria",
                "Migraine",
                "Tension Headache",
                "Vertigo",
                "Osteoarthritis",
                "Rheumatoid Arthritis",
                "Low Back Pain",
                "Cervical Spondylosis",
                "Peptic Ulcer Disease",
                "Irritable Bowel Syndrome (IBS)",
                "Constipation",
                "Diarrhoea",
                "Allergic Rhinitis",
                "Sinusitis",
                "Pharyngitis",
                "Tonsillitis",
                "Otitis Media",
                "Conjunctivitis",
                "Dermatitis",
                "Fungal Skin Infection",
                "Urticaria",
                "Chronic Obstructive Pulmonary Disease (COPD)",
                "Pneumonia",
                "Tuberculosis",
                "Hyperlipidemia / Dyslipidemia",
                "Obesity",
                "Gout",
                "Fibromyalgia",
                "Depression",
                "Anxiety Disorder",
                "Insomnia",
                "Epilepsy / Seizure Disorder",
                "Benign Prostatic Hyperplasia (BPH)",
                "Erectile Dysfunction",
                "Menstrual Irregularities",
                "Polycystic Ovary Syndrome (PCOS)",
                "Pregnancy - Antenatal Checkup",
                "Anaemia in Pregnancy",
                "Vitamin D Deficiency",
                "Vitamin B12 Deficiency",
                "Chickenpox",
                "Herpes Zoster (Shingles)",
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
