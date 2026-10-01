using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicPlanOfferRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicPlanOffer>(dbContext), IClinicPlanOfferRepository
{
    public async Task<IReadOnlyList<ClinicPlanOffer>> GetLiveForClinicAsync(
        Guid applicationId, DateTime utcNow, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.ApplicationId == applicationId
                     && o.IsActive
                     && (o.ValidUntil == null || o.ValidUntil >= utcNow))
            .ToListAsync(ct);

    public async Task<ClinicPlanOffer?> GetActiveForClinicAndPlanAsync(
        Guid applicationId, Guid subscriptionPlanId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ApplicationId == applicationId
                                   && o.SubscriptionPlanId == subscriptionPlanId
                                   && o.IsActive, ct);

    public async Task<ClinicPlanOffer?> GetByIdForClinicAsync(Guid id, Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(o => o.Id == id && o.ApplicationId == applicationId, ct);
}
