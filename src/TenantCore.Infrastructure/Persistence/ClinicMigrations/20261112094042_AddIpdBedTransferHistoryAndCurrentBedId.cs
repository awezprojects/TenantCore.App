using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddIpdBedTransferHistoryAndCurrentBedId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IpdMidStayChargeMode",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IpdRoomBillingCycle",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IpdBedTransferHistories",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpdRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePerDayAtTime = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OccupiedFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccupiedTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransferredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpdBedTransferHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpdBedTransferHistories_Beds_BedId",
                        column: x => x.BedId,
                        principalSchema: "clinic",
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpdBedTransferHistories_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "clinic",
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IpdBedTransferHistories_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "clinic",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IpdDeposits",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpdRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CollectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CounterSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpdDeposits", x => x.Id);
                    table.CheckConstraint("CK_IpdDeposits_Amount_Positive", "[Amount] > 0");
                });

            migrationBuilder.CreateTable(
                name: "IpdParticulars",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpdRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticularId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticularName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpdParticulars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpdPayments",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpdRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RoomChargesTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ParticularsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiscountedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeposited = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CounterSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundStatus = table.Column<int>(type: "int", nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpdPayments", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_IpdRegistrations_CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations",
                column: "CurrentBedId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdBedTransferHistories_ApplicationId_IpdRegistrationId",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                columns: new[] { "ApplicationId", "IpdRegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpdBedTransferHistories_BedId",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdBedTransferHistories_RoomId",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdBedTransferHistories_WardId",
                schema: "clinic",
                table: "IpdBedTransferHistories",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_IpdDeposits_ApplicationId_IpdRegistrationId",
                schema: "clinic",
                table: "IpdDeposits",
                columns: new[] { "ApplicationId", "IpdRegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpdParticulars_ApplicationId_IpdRegistrationId",
                schema: "clinic",
                table: "IpdParticulars",
                columns: new[] { "ApplicationId", "IpdRegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IpdPayments_IpdRegistrationId",
                schema: "clinic",
                table: "IpdPayments",
                column: "IpdRegistrationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IpdRegistrations_Beds_CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations",
                column: "CurrentBedId",
                principalSchema: "clinic",
                principalTable: "Beds",
                principalColumn: "Id");

            // ipd-billing-deposit-completion backfill (DEF-IPD-01/02/03/04 in the IPD E2E test
            // report). Every step is idempotent — safe to run more than once or against a
            // database this feature was never missing from.

            // 1) Resolve CurrentBedId from the existing free-text ward/room/bed names wherever
            // they match a real Bed in the same clinic. Rows that don't match (garbage data,
            // e.g. the test report's own "DUMMY ICU"/"ZZ-99") are left NULL for manual admin
            // reconciliation — this must not fail the migration.
            migrationBuilder.Sql(@"
                UPDATE i
                SET i.CurrentBedId = b.Id
                FROM [clinic].[IpdRegistrations] i
                INNER JOIN [clinic].[Wards] w ON w.ApplicationId = i.ApplicationId AND w.Name = i.WardName
                INNER JOIN [clinic].[Rooms] rm ON rm.WardId = w.Id AND rm.RoomNumber = i.RoomNumber
                INNER JOIN [clinic].[Beds] b ON b.RoomId = rm.Id AND b.BedNumber = i.BedNumber
                WHERE i.WardName IS NOT NULL AND i.RoomNumber IS NOT NULL AND i.BedNumber IS NOT NULL
                  AND i.CurrentBedId IS NULL;");

            // 2) Open one historical IpdBedTransferHistory segment per resolved registration,
            // spanning AdmissionDate -> DischargeDate (NULL if still admitted). RatePerDayAtTime
            // uses the room's CURRENT PricePerDay — no historical price snapshot exists before
            // this feature. Each segment is attributed to the admission's own DoctorUserId — no
            // separate "admitting staff" id was ever recorded on these pre-existing rows.
            migrationBuilder.Sql(@"
                INSERT INTO [clinic].[IpdBedTransferHistories]
                    (Id, ApplicationId, IpdRegistrationId, BedId, RoomId, WardId, RatePerDayAtTime, OccupiedFrom, OccupiedTo, TransferredByUserId, Notes, CreatedAt)
                SELECT NEWID(), i.ApplicationId, i.Id, i.CurrentBedId, rm.Id, w.Id, rm.PricePerDay, i.AdmissionDate, i.DischargeDate, i.DoctorUserId,
                       N'Backfilled by AddIpdBedTransferHistoryAndCurrentBedId migration.', SYSUTCDATETIME()
                FROM [clinic].[IpdRegistrations] i
                INNER JOIN [clinic].[Beds] b ON b.Id = i.CurrentBedId
                INNER JOIN [clinic].[Rooms] rm ON rm.Id = b.RoomId
                INNER JOIN [clinic].[Wards] w ON w.Id = rm.WardId
                WHERE i.CurrentBedId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [clinic].[IpdBedTransferHistories] h WHERE h.IpdRegistrationId = i.Id);");

            // 3) DEF-IPD-04: an admission left in the Transferred status trap (Status = 3) by the
            // defect can never be discharged again — reset it to Admitted (1).
            migrationBuilder.Sql(@"UPDATE [clinic].[IpdRegistrations] SET Status = 1 WHERE Status = 3;");

            // 4) DEF-IPD-01 cleanup: release any bed still marked occupied with no Admitted
            // registration actually pointing at it — the "ghost bed" the defect left behind
            // (exactly what the test report's own leftover QA-B1 bed demonstrates).
            migrationBuilder.Sql(@"
                UPDATE b
                SET b.IsOccupied = 0
                FROM [clinic].[Beds] b
                WHERE b.IsOccupied = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM [clinic].[IpdRegistrations] i
                      WHERE i.CurrentBedId = b.Id AND i.Status = 1
                  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The Status=3->1 reset and the ghost-bed IsOccupied cleanup from Up()'s backfill are
            // one-way data corrections — dropping the tables/columns below removes the backfilled
            // CurrentBedId/history rows along with everything else, but does not (and should not)
            // restore the pre-fix Status/IsOccupied values.
            migrationBuilder.DropForeignKey(
                name: "FK_IpdRegistrations_Beds_CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropTable(
                name: "IpdBedTransferHistories",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "IpdDeposits",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "IpdParticulars",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "IpdPayments",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "IX_IpdRegistrations_CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropColumn(
                name: "CurrentBedId",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropColumn(
                name: "IpdMidStayChargeMode",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.DropColumn(
                name: "IpdRoomBillingCycle",
                schema: "clinic",
                table: "ClinicFeatureFlags");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 919, DateTimeKind.Utc).AddTicks(6205));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 919, DateTimeKind.Utc).AddTicks(6212));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 919, DateTimeKind.Utc).AddTicks(6242));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 919, DateTimeKind.Utc).AddTicks(6246));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 923, DateTimeKind.Utc).AddTicks(171));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 923, DateTimeKind.Utc).AddTicks(182));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 923, DateTimeKind.Utc).AddTicks(186));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 11, 4, 1, 3, 35, 923, DateTimeKind.Utc).AddTicks(189));
        }
    }
}
