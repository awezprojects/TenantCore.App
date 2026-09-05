using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicLocationRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicLocation>(dbContext), IClinicLocationRepository
{
    // Not AsNoTracking — reused by the upsert handler, which needs a tracked entity to call Update() on.
    public async Task<ClinicLocation?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Include(l => l.State)
            .Include(l => l.City)
            .FirstOrDefaultAsync(l => l.ApplicationId == applicationId, ct);
}
