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

    // Must cover every IsClinicLink purpose (Renewal, AdminAssigned, WalletTopUp, RegistrationFee) —
    // this is the single query all six "create a link" handlers share to enforce "at most one open
    // clinic link at a time". Previously only matched Renewal/AdminAssigned, so a wallet top-up or
    // registration fee link was never found as "already open": every repeat click created a brand
    // new SubscriptionPayment row and a brand new Razorpay link instead of returning the existing
    // one, and the cross-purpose "one open link" invariant didn't actually hold for those two.
    public async Task<SubscriptionPayment?> GetOpenLinkForClinicAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Where(p => p.ApplicationId == applicationId
                     && (p.Purpose == PaymentPurpose.Renewal || p.Purpose == PaymentPurpose.AdminAssigned
                      || p.Purpose == PaymentPurpose.WalletTopUp || p.Purpose == PaymentPurpose.RegistrationFee)
                     && (p.Status == SubscriptionPaymentStatus.Pending || p.Status == SubscriptionPaymentStatus.LinkCreated))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<SubscriptionPayment?> GetByIdForClinicAsync(Guid id, Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.Id == id && p.ApplicationId == applicationId, ct);

    public async Task<SubscriptionPayment?> GetLatestRegistrationFeePaymentAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId
                     && p.Purpose == PaymentPurpose.RegistrationFee
                     && (p.Status == SubscriptionPaymentStatus.Pending
                      || p.Status == SubscriptionPaymentStatus.LinkCreated
                      || p.Status == SubscriptionPaymentStatus.Expired))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
