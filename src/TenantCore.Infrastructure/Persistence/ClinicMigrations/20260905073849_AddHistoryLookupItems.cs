using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddHistoryLookupItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoryLookupItems",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryLookupItems", x => x.Id);
                });

            SeedGlobalDefaults(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 406, DateTimeKind.Utc).AddTicks(9574));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 406, DateTimeKind.Utc).AddTicks(9580));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 406, DateTimeKind.Utc).AddTicks(9585));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 406, DateTimeKind.Utc).AddTicks(9587));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 407, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 407, DateTimeKind.Utc).AddTicks(8546));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 407, DateTimeKind.Utc).AddTicks(8549));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 7, 38, 47, 407, DateTimeKind.Utc).AddTicks(8572));

            migrationBuilder.CreateIndex(
                name: "IX_HistoryLookupItems_ApplicationId_Type_Value",
                schema: "clinic",
                table: "HistoryLookupItems",
                columns: new[] { "ApplicationId", "Type", "Value" },
                unique: true,
                filter: "[ApplicationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryLookupItems_Type",
                schema: "clinic",
                table: "HistoryLookupItems",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryLookupItems",
                schema: "clinic");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 927, DateTimeKind.Utc).AddTicks(6915));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 927, DateTimeKind.Utc).AddTicks(6918));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 927, DateTimeKind.Utc).AddTicks(6919));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 927, DateTimeKind.Utc).AddTicks(6920));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 928, DateTimeKind.Utc).AddTicks(720));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 928, DateTimeKind.Utc).AddTicks(725));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 928, DateTimeKind.Utc).AddTicks(726));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 6, 44, 19, 928, DateTimeKind.Utc).AddTicks(738));
        }

        // 20 common, doctor-curated defaults per history type — global (ApplicationId = null),
        // so every clinic can use them out of the box. Clinics add their own via the app,
        // which get their own ApplicationId and are only visible to that clinic.
        private static void SeedGlobalDefaults(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            SeedType(migrationBuilder, "e1000000", 1, seedTime,
            [
                "3-4/30 Regular",
                "Moderate flow",
                "Scanty flow",
                "Heavy flow",
                "Irregular cycles",
                "Dysmenorrhea",
                "Amenorrhea",
                "Oligomenorrhea",
                "Menorrhagia",
                "Metrorrhagia",
                "Polymenorrhea",
                "Regular cycles, no complaints",
                "Post-menopausal",
                "Premenarchal",
                "Intermenstrual bleeding",
                "Clots present",
                "No clots",
                "Painless periods",
                "Painful periods, relieved by analgesics",
                "LMP normal, no abnormal bleeding",
            ]);

            SeedType(migrationBuilder, "e2000000", 2, seedTime,
            [
                "No significant past medical history",
                "Hypertension",
                "Diabetes Mellitus",
                "Hypothyroidism",
                "Hyperthyroidism",
                "Asthma",
                "Anaemia",
                "Epilepsy",
                "Cardiac disease",
                "Tuberculosis (treated)",
                "Thyroid disorder",
                "Gestational diabetes (previous pregnancy)",
                "PCOS",
                "Previous blood transfusion",
                "Rh negative",
                "Previous cesarean section",
                "Previous miscarriage",
                "Known drug allergy",
                "Renal disease",
                "Liver disease",
            ]);

            SeedType(migrationBuilder, "e3000000", 3, seedTime,
            [
                "Not Significant",
                "Hypertension",
                "Diabetes Mellitus",
                "Cancer (specify)",
                "Twin pregnancy",
                "Hereditary disorder",
                "Thyroid disorder",
                "Bleeding disorder",
                "Congenital anomalies",
                "Thalassemia trait",
                "Consanguineous marriage",
                "Cardiac disease",
                "Tuberculosis",
                "Asthma",
                "Mental illness",
                "Epilepsy",
                "Preeclampsia in family",
                "Recurrent miscarriages in family",
                "Multiple pregnancy in family",
                "No known hereditary disease",
            ]);

            SeedType(migrationBuilder, "e4000000", 4, seedTime,
            [
                "Uterus corresponds to POG",
                "Fundal height corresponds to POG",
                "Fundal height less than POG",
                "Fundal height more than POG",
                "Single live intrauterine fetus",
                "Cephalic presentation",
                "Breech presentation",
                "Transverse lie",
                "Longitudinal lie",
                "FHS heard, regular, good volume",
                "FHS not heard",
                "Head engaged",
                "Head not engaged, floating",
                "Adequate liquor",
                "Reduced liquor (oligohydramnios)",
                "Increased liquor (polyhydramnios)",
                "Uterus soft, non-tender",
                "Scar tenderness present",
                "No scar tenderness",
                "Uterine contractions present",
            ]);

            SeedType(migrationBuilder, "e5000000", 5, seedTime,
            [
                "Os closed",
                "Os admits fingertip",
                "Cervix 2 cm dilated",
                "Cervix 4 cm dilated",
                "Cervix 6 cm dilated",
                "Cervix fully dilated",
                "Cervix effaced 50%",
                "Cervix effaced 80%",
                "Cervix soft and central",
                "Cervix firm and posterior",
                "Membranes intact",
                "Membranes ruptured",
                "Vertex presentation",
                "Breech presentation",
                "Head at station -2",
                "Head at station 0",
                "Head at station +2",
                "No bleeding per vaginum",
                "Bleeding per vaginum present",
                "No foul smelling discharge",
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
