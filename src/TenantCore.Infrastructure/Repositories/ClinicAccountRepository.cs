using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicAccountRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicAccount>(dbContext), IClinicAccountRepository
{
    public async Task<ClinicAccount?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);

    public async Task<bool> IsSuspendedAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(a => a.ApplicationId == applicationId
                        && a.AccessStatus == ClinicAccessStatus.Suspended, ct);

    public async Task<BillingMode> GetBillingModeAsync(Guid applicationId, CancellationToken ct = default)
    {
        // Three columns, no tracking, hit on every guarded clinic-scoped request. Projecting rather
        // than loading the row keeps the guard cheap.
        var row = await DbSet.AsNoTracking()
            .Where(a => a.ApplicationId == applicationId)
            .Select(a => new { a.BillingMode, a.PendingBillingMode, a.PendingBillingModeEffectiveFrom })
            .FirstOrDefaultAsync(ct);

        // No row means the clinic has never needed one — Subscription, exactly as before Phase 2.
        if (row is null)
            return BillingMode.Subscription;

        // The rule itself lives on ClinicAccount, so the guard and the UI cannot disagree about which
        // mode is live.
        return ClinicAccount.ResolveEffectiveBillingMode(
            row.BillingMode, row.PendingBillingMode, row.PendingBillingModeEffectiveFrom, DateTime.UtcNow);
    }

    public async Task<ClinicAccount> GetOrCreateAsync(Guid applicationId, CancellationToken ct = default)
    {
        var account = await GetByApplicationIdAsync(applicationId, ct);
        if (account is not null)
            return account;

        account = ClinicAccount.CreateDefault(applicationId);
        await DbSet.AddAsync(account, ct);
        return account;
    }
}
