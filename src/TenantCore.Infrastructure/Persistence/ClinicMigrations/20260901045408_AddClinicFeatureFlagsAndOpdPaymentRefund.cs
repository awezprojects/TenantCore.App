using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddClinicFeatureFlagsAndOpdPaymentRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundDue",
                schema: "clinic",
                table: "OpdPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RefundStatus",
                schema: "clinic",
                table: "OpdPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                schema: "clinic",
                table: "OpdPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundedByUserId",
                schema: "clinic",
                table: "OpdPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicFeatureFlags",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrepaidOpdEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicFeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeatureFlags_ApplicationId",
                schema: "clinic",
                table: "ClinicFeatureFlags",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicFeatureFlags",
                schema: "clinic");

            migrationBuilder.DropColumn(
                name: "RefundDue",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropColumn(
                name: "RefundedByUserId",
                schema: "clinic",
                table: "OpdPayments");
        }
    }
}
