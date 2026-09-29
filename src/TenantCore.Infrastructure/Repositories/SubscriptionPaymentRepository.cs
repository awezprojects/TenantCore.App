using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class SubscriptionPaymentRepository(ClinicDbContext dbContext)
    : ClinicRepository<SubscriptionPayment>(dbContext), ISubscriptionPaymentRepository
{
    public async Task<SubscriptionPayment?> GetByGatewayLinkIdAsync(string gatewayPaymentLinkId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.GatewayPaymentLinkId == gatewayPaymentLinkId, ct);

    public async Task<IReadOnlyList<SubscriptionPayment>> GetDueForReconciliationAsync(DateTime olderThan, int batchSize, CancellationToken ct = default)
        => await DbSet
            .Where(p => p.Status == SubscriptionPaymentStatus.LinkCreated && p.CreatedAt < olderThan)
            .OrderBy(p => p.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubscriptionPayment>> GetForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(p => p.ApplicationId == applicationId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<SubscriptionPayment?> GetOpenRenewalForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Where(p => p.ApplicationId == applicationId && p.Purpose == PaymentPurpose.Renewal
                     && (p.Status == SubscriptionPaymentStatus.Pending || p.Status == SubscriptionPaymentStatus.LinkCreated))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
