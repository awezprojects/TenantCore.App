using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class BackfillOpdPaymentCollectedAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEF-E2E-09: rows collected before OpdPayment.RecordItemCollection existed (see the
            // AddOpdParticularPaymentTracking migration's own backfill for the same reasoning)
            // never got their individually collected items folded into CollectedAmount — it was
            // left at just the visit fee. Recompute it from the visit fee plus whatever items
            // were already marked collected. Restricted to Discount = 0: a discounted payment's
            // CollectedAmount is set wholesale by AcceptFull (and reduced by ProcessRefund), so it
            // is already correct and this formula does not apply to it. The recomputation is
            // idempotent — rows already correct (round 2/3, created after the code fix) get the
            // same value back — so this is safe to run more than once or on an unaffected database.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.CollectedAmount = p.VisitFee + ISNULL((
                    SELECT SUM(op.Amount)
                    FROM [clinic].[OpdParticulars] op
                    WHERE op.OpdRegistrationId = p.OpdRegistrationId
                      AND op.PaymentStatus = 2
                      AND op.CounterSessionId IS NOT NULL
                ), 0)
                FROM [clinic].[OpdPayments] p
                WHERE p.PaymentStatus = 2 AND p.Discount = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // One-way data correction — the original (broken, visit-fee-only) CollectedAmount
            // values aren't recoverable, and there is no reason to want them back.
        }
    }
}
