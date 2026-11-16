using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdDepositRepository(ClinicDbContext dbContext) : ClinicRepository<IpdDeposit>(dbContext), IIpdDepositRepository
{
    public async Task<IEnumerable<IpdDeposit>> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(d => d.IpdRegistrationId == ipdRegistrationId && d.ApplicationId == applicationId)
            .OrderBy(d => d.CollectedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<IpdDeposit>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(d => d.ApplicationId == applicationId && d.CollectedAt >= fromUtc && d.CollectedAt < toUtc)
            .ToListAsync(ct);
}
