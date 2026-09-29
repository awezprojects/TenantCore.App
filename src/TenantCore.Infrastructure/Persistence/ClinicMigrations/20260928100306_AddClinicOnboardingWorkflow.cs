using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddClinicOnboardingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OnboardingRequestId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedByUserId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPaymentId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicOnboardingRequests",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequesterEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequesterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClinicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreferredClinicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClinicContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OfficialEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MedicalRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MedicalCouncil = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExpectedStaffCount = table.Column<int>(type: "int", nullable: false),
                    ReferralSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedPlanCode = table.Column<int>(type: "int", nullable: true),
                    PlanListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AmountReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsTrialGrant = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovedClinicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvisionedApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPaymentCheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NeedsAttention = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AttentionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicOnboardingRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gateway = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    OnboardingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanCode = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountInMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PlanListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacesPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacedByPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GatewayPaymentLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaymentLinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LinkExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GatewayPaymentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClinicSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPayments_Amount_NonNegative", "[Amount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTasks",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTasks", x => x.Id);
                });

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
                name: "IX_ClinicSubscriptions_OnboardingRequestId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                column: "OnboardingRequestId",
                unique: true,
                filter: "[OnboardingRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSubscriptions_SubscriptionPaymentId",
                schema: "clinic",
                table: "ClinicSubscriptions",
                column: "SubscriptionPaymentId",
                unique: true,
                filter: "[SubscriptionPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOnboardingRequests_NeedsAttention",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                column: "NeedsAttention");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOnboardingRequests_RequestedByUserId",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOnboardingRequests_Status",
                schema: "clinic",
                table: "ClinicOnboardingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_EventId",
                schema: "clinic",
                table: "PaymentWebhookEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_ApplicationId",
                schema: "clinic",
                table: "SubscriptionPayments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_ClinicSubscriptionId",
                schema: "clinic",
                table: "SubscriptionPayments",
                column: "ClinicSubscriptionId",
                unique: true,
                filter: "[ClinicSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_GatewayPaymentId",
                schema: "clinic",
                table: "SubscriptionPayments",
                column: "GatewayPaymentId",
                unique: true,
                filter: "[GatewayPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_GatewayPaymentLinkId",
                schema: "clinic",
                table: "SubscriptionPayments",
                column: "GatewayPaymentLinkId",
                unique: true,
                filter: "[GatewayPaymentLinkId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_OnboardingRequestId",
                schema: "clinic",
                table: "SubscriptionPayments",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_Status_CreatedAt",
                schema: "clinic",
                table: "SubscriptionPayments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_AggregateType_AggregateId",
                schema: "clinic",
                table: "WorkflowTasks",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_IdempotencyKey",
                schema: "clinic",
                table: "WorkflowTasks",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_Status_NextAttemptAt",
                schema: "clinic",
                table: "WorkflowTasks",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicOnboardingRequests",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "WorkflowTasks",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "IX_ClinicSubscriptions_OnboardingRequestId",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ClinicSubscriptions_SubscriptionPaymentId",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "OnboardingRequestId",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "PurchasedByUserId",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionPaymentId",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(639));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(641));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(646));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(647));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(5253));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(5272));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(5274));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 13, 23, 41, 50, 749, DateTimeKind.Utc).AddTicks(5276));
        }
    }
}
