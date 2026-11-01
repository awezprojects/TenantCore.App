using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class PaymentWebhookEventRepository(ClinicDbContext dbContext)
    : ClinicRepository<PaymentWebhookEvent>(dbContext), IPaymentWebhookEventRepository
{
    public async Task<bool> TryAddAsync(PaymentWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        var exists = await DbSet.AnyAsync(e => e.EventId == webhookEvent.EventId, ct);
        if (exists)
            return false;

        await DbSet.AddAsync(webhookEvent, ct);
        return true;
    }

    /// <summary>
    /// The pre-check in TryAddAsync is a check-then-add, not atomic — two genuinely concurrent
    /// deliveries of the same webhook event can both pass it before either commits. The unique
    /// index on EventId is the real backstop; this treats losing that race exactly like the
    /// pre-check catching it, instead of surfacing an unhandled DbUpdateException as a 500.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateEventIdViolation(ex))
        {
            return 0;
        }
    }

    private static bool IsDuplicateEventIdViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_PaymentWebhookEvents_EventId", StringComparison.OrdinalIgnoreCase) == true;
}
