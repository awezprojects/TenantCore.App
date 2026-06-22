using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ParticularRepository(ClinicDbContext dbContext) : ClinicRepository<Particular>(dbContext), IParticularRepository
{
    public async Task<IEnumerable<Particular>> GetActiveAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
}
