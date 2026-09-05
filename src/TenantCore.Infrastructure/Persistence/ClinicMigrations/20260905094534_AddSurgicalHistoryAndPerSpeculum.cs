using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddSurgicalHistoryAndPerSpeculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PerSpeculum",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurgicalHistory",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                type: "nvarchar(max)",
                nullable: true);

            SeedGlobalDefaults(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2645));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2647));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2650));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2651));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6865));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6870));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6872));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6874));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [clinic].[HistoryLookupItems] WHERE [Type] IN (6, 7) AND [ApplicationId] IS NULL");

            migrationBuilder.DropColumn(
                name: "PerSpeculum",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.DropColumn(
                name: "SurgicalHistory",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(666));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(670));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(674));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(677));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(9459));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(9468));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(9489));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 8, 4, 54, 69, DateTimeKind.Utc).AddTicks(9493));
        }

        // 20 common, doctor-curated defaults for the 2 new history types — global
        // (ApplicationId = null), so every clinic can use them out of the box.
        private static void SeedGlobalDefaults(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            SeedType(migrationBuilder, "e6000000", 6, seedTime,
            [
                "No previous surgery",
                "Previous LSCS",
                "Previous 2 LSCS",
                "Tubal ligation",
                "Appendectomy",
                "Cholecystectomy",
                "Ovarian cystectomy",
                "Myomectomy",
                "Hysterectomy",
                "D&C (Dilatation and Curettage)",
                "Cervical cerclage",
                "Laparoscopic surgery",
                "Hernia repair",
                "Ectopic pregnancy surgery",
                "Previous perineal repair",
                "Previous instrumental delivery",
                "Previous classical cesarean section",
                "Thyroid surgery",
                "No known complications from previous surgery",
                "Previous blood transfusion during surgery",
            ]);

            SeedType(migrationBuilder, "e7000000", 7, seedTime,
            [
                "Cervix healthy",
                "Cervix congested",
                "Cervix erosion present",
                "Os closed",
                "Os open",
                "Discharge - normal",
                "Discharge - white curdy",
                "Discharge - foul smelling",
                "Discharge - blood stained",
                "No bleeding seen",
                "Bleeding seen through os",
                "Vaginal walls healthy",
                "Vaginal walls congested",
                "Cervical polyp seen",
                "No polyp seen",
                "Nabothian cysts present",
                "Cervical growth suspicious",
                "Bluish discoloration of cervix (Chadwick sign)",
                "No abnormal discharge",
                "Speculum exam deferred",
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
