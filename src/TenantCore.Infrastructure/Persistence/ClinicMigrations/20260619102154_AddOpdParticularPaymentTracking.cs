using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddOpdParticularPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CollectedAmount",
                schema: "clinic",
                table: "OpdPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollectedAt",
                schema: "clinic",
                table: "OpdParticulars",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CollectedByUserId",
                schema: "clinic",
                table: "OpdParticulars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterSessionId",
                schema: "clinic",
                table: "OpdParticulars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                schema: "clinic",
                table: "OpdParticulars",
                type: "int",
                nullable: false,
                defaultValue: 1); // 1 = Pending

            // All existing particulars are Pending (never individually tracked before).
            migrationBuilder.Sql(
                "UPDATE [clinic].[OpdParticulars] SET PaymentStatus = 1");

            // Existing received payments were collected at FinalAmount under the old model.
            migrationBuilder.Sql(
                "UPDATE [clinic].[OpdPayments] SET CollectedAmount = FinalAmount WHERE PaymentStatus = 2");

            migrationBuilder.CreateIndex(
                name: "IX_OpdParticulars_CounterSessionId",
                schema: "clinic",
                table: "OpdParticulars",
                column: "CounterSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpdParticulars_CounterSessionId",
                schema: "clinic",
                table: "OpdParticulars");

            migrationBuilder.DropColumn(
                name: "CollectedAmount",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropColumn(
                name: "CollectedAt",
                schema: "clinic",
                table: "OpdParticulars");

            migrationBuilder.DropColumn(
                name: "CollectedByUserId",
                schema: "clinic",
                table: "OpdParticulars");

            migrationBuilder.DropColumn(
                name: "CounterSessionId",
                schema: "clinic",
                table: "OpdParticulars");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "clinic",
                table: "OpdParticulars");
        }
    }
}
