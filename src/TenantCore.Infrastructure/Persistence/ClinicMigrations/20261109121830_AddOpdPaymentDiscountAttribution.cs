using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddOpdPaymentDiscountAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountedAt",
                schema: "clinic",
                table: "OpdPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DiscountedByUserId",
                schema: "clinic",
                table: "OpdPayments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountedAt",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropColumn(
                name: "DiscountedByUserId",
                schema: "clinic",
                table: "OpdPayments");
        }
    }
}
