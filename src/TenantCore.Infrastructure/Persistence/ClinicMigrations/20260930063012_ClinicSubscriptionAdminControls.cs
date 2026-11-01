using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class ClinicSubscriptionAdminControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "clinic",
                table: "SubscriptionPlans");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                schema: "clinic",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AmountReason",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicName",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatedByAdminEmail",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckAt",
                schema: "clinic",
                table: "SubscriptionPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrantReason",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrantedByAdminEmail",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicAccounts",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessStatus = table.Column<int>(type: "int", nullable: false),
                    SuspensionMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReactivatedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RestrictToOfferedPlans = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicPlanOffers",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WithdrawnByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicPlanOffers", x => x.Id);
                    table.CheckConstraint("CK_ClinicPlanOffers_OfferPrice_NonNegative", "[OfferPrice] IS NULL OR [OfferPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_ClinicPlanOffers_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalSchema: "clinic",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 6, 30, 10, 90, DateTimeKind.Utc).AddTicks(8230));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 6, 30, 10, 90, DateTimeKind.Utc).AddTicks(8233));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 6, 30, 10, 90, DateTimeKind.Utc).AddTicks(8236));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 30, 6, 30, 10, 90, DateTimeKind.Utc).AddTicks(8265));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "IsPublic" },
                values: new object[] { new DateTime(2026, 9, 30, 6, 30, 10, 93, DateTimeKind.Utc).AddTicks(2551), true });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "IsPublic" },
                values: new object[] { new DateTime(2026, 9, 30, 6, 30, 10, 93, DateTimeKind.Utc).AddTicks(2558), true });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "IsPublic" },
                values: new object[] { new DateTime(2026, 9, 30, 6, 30, 10, 93, DateTimeKind.Utc).AddTicks(2575), true });

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "IsPublic" },
                values: new object[] { new DateTime(2026, 9, 30, 6, 30, 10, 93, DateTimeKind.Utc).AddTicks(2578), true });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "clinic",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true,
                filter: "[Code] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSubscriptions_ApplicationId_Status_StartDate",
                schema: "clinic",
                table: "ClinicSubscriptions",
                columns: new[] { "ApplicationId", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAccounts_ApplicationId",
                schema: "clinic",
                table: "ClinicAccounts",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPlanOffers_ApplicationId_IsActive",
                schema: "clinic",
                table: "ClinicPlanOffers",
                columns: new[] { "ApplicationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPlanOffers_ApplicationId_SubscriptionPlanId",
                schema: "clinic",
                table: "ClinicPlanOffers",
                columns: new[] { "ApplicationId", "SubscriptionPlanId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPlanOffers_SubscriptionPlanId",
                schema: "clinic",
                table: "ClinicPlanOffers",
                column: "SubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicAccounts",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "ClinicPlanOffers",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "clinic",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_ClinicSubscriptions_ApplicationId_Status_StartDate",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                schema: "clinic",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "AmountReason",
                schema: "clinic",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "ClinicName",
                schema: "clinic",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "InitiatedByAdminEmail",
                schema: "clinic",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "LastCheckAt",
                schema: "clinic",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "GrantReason",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "GrantedByAdminEmail",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 968, DateTimeKind.Utc).AddTicks(998));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 968, DateTimeKind.Utc).AddTicks(1004));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 968, DateTimeKind.Utc).AddTicks(1009));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 968, DateTimeKind.Utc).AddTicks(1028));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 970, DateTimeKind.Utc).AddTicks(6346));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 970, DateTimeKind.Utc).AddTicks(6353));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 970, DateTimeKind.Utc).AddTicks(6371));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 28, 10, 3, 3, 970, DateTimeKind.Utc).AddTicks(6374));

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "clinic",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);
        }
    }
}
