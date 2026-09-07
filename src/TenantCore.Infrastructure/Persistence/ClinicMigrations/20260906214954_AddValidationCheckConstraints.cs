using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddValidationCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 777, DateTimeKind.Utc).AddTicks(5455));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 777, DateTimeKind.Utc).AddTicks(5458));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 777, DateTimeKind.Utc).AddTicks(5462));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 777, DateTimeKind.Utc).AddTicks(5471));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 778, DateTimeKind.Utc).AddTicks(1710));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 778, DateTimeKind.Utc).AddTicks(1731));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 778, DateTimeKind.Utc).AddTicks(1745));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 21, 49, 52, 778, DateTimeKind.Utc).AddTicks(1747));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Rooms_PricePerDay_NonNegative",
                schema: "clinic",
                table: "Rooms",
                sql: "[PricePerDay] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PregnancyTenures_Lmp_Range",
                table: "PregnancyTenures",
                sql: "[Lmp] <= CAST(GETUTCDATE() AS date) AND [Lmp] >= DATEADD(MONTH, -10, CAST(GETUTCDATE() AS date))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Particulars_DefaultAmount_NonNegative",
                schema: "clinic",
                table: "Particulars",
                sql: "[DefaultAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_CollectedAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[CollectedAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_Discount_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[Discount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_FinalAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[FinalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_ParticularsTotal_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[ParticularsTotal] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_RefundDue_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[RefundDue] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_TotalAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[TotalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdPayments_VisitFee_NonNegative",
                schema: "clinic",
                table: "OpdPayments",
                sql: "[VisitFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpdParticulars_Amount_NonNegative",
                schema: "clinic",
                table: "OpdParticulars",
                sql: "[Amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ObstetricPrescriptionData_Counts_NonNegative",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                sql: "(Gravida IS NULL OR Gravida >= 0) AND (Para IS NULL OR Para >= 0) AND (Live IS NULL OR Live >= 0) AND (Abortion IS NULL OR Abortion >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ObstetricPrescriptionData_Lmp_Range",
                schema: "clinic",
                table: "ObstetricPrescriptionData",
                sql: "[Lmp] IS NULL OR ([Lmp] <= CAST(GETUTCDATE() AS date) AND [Lmp] >= DATEADD(MONTH, -10, CAST(GETUTCDATE() AS date)))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpdRegistrations_InitialFee_NonNegative",
                schema: "clinic",
                table: "IpdRegistrations",
                sql: "[InitialFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExpenseRecords_Amount_NonNegative",
                schema: "clinic",
                table: "ExpenseRecords",
                sql: "[Amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExpenseRecords_PaidAmount_NonNegative",
                schema: "clinic",
                table: "ExpenseRecords",
                sql: "[PaidAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DoctorFeeConfigs_VisitFee_NonNegative",
                schema: "clinic",
                table: "DoctorFeeConfigs",
                sql: "[VisitFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClinicSubscriptions_DurationDays_Positive",
                schema: "clinic",
                table: "ClinicSubscriptions",
                sql: "[DurationDays] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClinicSubscriptions_PricePaid_NonNegative",
                schema: "clinic",
                table: "ClinicSubscriptions",
                sql: "[PricePaid] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClinicFeeConfigs_OpdFee_NonNegative",
                schema: "clinic",
                table: "ClinicFeeConfigs",
                sql: "[OpdFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AmountHandovers_Amount_NonNegative",
                schema: "clinic",
                table: "AmountHandovers",
                sql: "[Amount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Rooms_PricePerDay_NonNegative",
                schema: "clinic",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PregnancyTenures_Lmp_Range",
                table: "PregnancyTenures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Particulars_DefaultAmount_NonNegative",
                schema: "clinic",
                table: "Particulars");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_CollectedAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_Discount_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_FinalAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_ParticularsTotal_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_RefundDue_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_TotalAmount_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdPayments_VisitFee_NonNegative",
                schema: "clinic",
                table: "OpdPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpdParticulars_Amount_NonNegative",
                schema: "clinic",
                table: "OpdParticulars");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ObstetricPrescriptionData_Counts_NonNegative",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ObstetricPrescriptionData_Lmp_Range",
                schema: "clinic",
                table: "ObstetricPrescriptionData");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpdRegistrations_InitialFee_NonNegative",
                schema: "clinic",
                table: "IpdRegistrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExpenseRecords_Amount_NonNegative",
                schema: "clinic",
                table: "ExpenseRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExpenseRecords_PaidAmount_NonNegative",
                schema: "clinic",
                table: "ExpenseRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DoctorFeeConfigs_VisitFee_NonNegative",
                schema: "clinic",
                table: "DoctorFeeConfigs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClinicSubscriptions_DurationDays_Positive",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClinicSubscriptions_PricePaid_NonNegative",
                schema: "clinic",
                table: "ClinicSubscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClinicFeeConfigs_OpdFee_NonNegative",
                schema: "clinic",
                table: "ClinicFeeConfigs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AmountHandovers_Amount_NonNegative",
                schema: "clinic",
                table: "AmountHandovers");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 598, DateTimeKind.Utc).AddTicks(9296));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 598, DateTimeKind.Utc).AddTicks(9303));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 598, DateTimeKind.Utc).AddTicks(9307));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 598, DateTimeKind.Utc).AddTicks(9322));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 600, DateTimeKind.Utc).AddTicks(5715));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 600, DateTimeKind.Utc).AddTicks(5728));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 600, DateTimeKind.Utc).AddTicks(5755));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 6, 18, 32, 5, 600, DateTimeKind.Utc).AddTicks(5760));
        }
    }
}
