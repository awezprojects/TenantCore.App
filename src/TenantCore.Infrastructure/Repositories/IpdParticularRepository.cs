using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdParticularRepository(ClinicDbContext dbContext) : ClinicRepository<IpdParticular>(dbContext), IIpdParticularRepository
{
    public async Task<IEnumerable<IpdParticular>> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(p => p.IpdRegistrationId == ipdRegistrationId && p.ApplicationId == applicationId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
}
