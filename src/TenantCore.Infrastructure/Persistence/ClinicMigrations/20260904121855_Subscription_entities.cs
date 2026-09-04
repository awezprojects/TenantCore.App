using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class Subscription_entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionAlertSettings",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    DaysBeforeExpiry = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BodyMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionAlertSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "INR"),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPopular = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSubscriptions",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanCode = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PricePaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClinicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BillingContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BillingContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalSchema: "clinic",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                columns: new[] { "Id", "AlertType", "BodyMessage", "CreatedAt", "CreatedBy", "DaysBeforeExpiry", "DisplayOrder", "Headline", "IsEnabled", "Subject", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("c3d4e5f6-0001-0000-0000-000000000000"), 1, "Your subscription is set to expire on {ExpiryDate}. Renew now to avoid any interruption to your clinic's access.", new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(2471), null, 10, 1, "Time to renew soon", true, "Your {ClinicName} subscription expires in {DaysRemaining} days", null, null },
                    { new Guid("c3d4e5f6-0002-0000-0000-000000000000"), 1, "Only a few days left. Renew before {ExpiryDate} to keep your clinic running without interruption.", new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(2473), null, 5, 2, "Your subscription expires soon", true, "Reminder: {ClinicName} subscription expires in {DaysRemaining} days", null, null },
                    { new Guid("c3d4e5f6-0003-0000-0000-000000000000"), 1, "Your subscription expires on {ExpiryDate}. Renew today to avoid losing access to your clinic.", new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(2475), null, 2, 3, "Final reminder — act now", true, "Final notice: {ClinicName} subscription expires in {DaysRemaining} days", null, null },
                    { new Guid("c3d4e5f6-0004-0000-0000-000000000000"), 2, "Your subscription expired on {ExpiryDate}. Choose a plan to restore access to your clinic.", new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(2476), null, 0, 4, "Subscription expired", true, "Your {ClinicName} subscription has expired", null, null }
                });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Currency", "Description", "DisplayOrder", "DurationDays", "IsActive", "IsTrial", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("b2c3d4e5-0001-0000-0000-000000000000"), 1, new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(5945), null, "INR", "Try every feature free for 14 days.", 1, 14, true, true, "Free Trial", 0m, null, null });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Currency", "Description", "DisplayOrder", "DurationDays", "IsActive", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("b2c3d4e5-0002-0000-0000-000000000000"), 2, new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(5957), null, "INR", "Billed every 30 days. Cancel anytime.", 2, 30, true, "Monthly", 999m, null, null });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Currency", "Description", "DisplayOrder", "DurationDays", "IsActive", "IsPopular", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("b2c3d4e5-0003-0000-0000-000000000000"), 3, new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(5959), null, "INR", "Our most popular plan — save versus monthly billing.", 3, 90, true, true, "Quarterly", 2499m, null, null });

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Currency", "Description", "DisplayOrder", "DurationDays", "IsActive", "Name", "Price", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("b2c3d4e5-0004-0000-0000-000000000000"), 4, new DateTime(2026, 9, 4, 12, 18, 54, 513, DateTimeKind.Utc).AddTicks(5960), null, "INR", "The best value — a full year of every feature.", 4, 365, true, "Yearly", 8999m, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSubscriptions_ApplicationId_Status_EndDate",
                schema: "clinic",
                table: "ClinicSubscriptions",
                columns: new[] { "ApplicationId", "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSubscriptions_SubscriptionPlanId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAlertSettings_AlertType_DaysBeforeExpiry",
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                columns: new[] { "AlertType", "DaysBeforeExpiry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "clinic",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSubscriptions",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "SubscriptionAlertSettings",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "clinic");
        }
    }
}
