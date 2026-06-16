using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddObstetricPrescriptionDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ObstetricPrescriptionData",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gravida = table.Column<int>(type: "int", nullable: true),
                    Para = table.Column<int>(type: "int", nullable: true),
                    Live = table.Column<int>(type: "int", nullable: true),
                    Abortion = table.Column<int>(type: "int", nullable: true),
                    Information = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MenstrualHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PastMedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObstetricPrescriptionData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObstetricPrescriptionData_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalSchema: "clinic",
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObstetricPrescriptionData_PrescriptionId",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                column: "PrescriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObstetricPrescriptionData",
                schema: "clinic");
        }
    }
}
