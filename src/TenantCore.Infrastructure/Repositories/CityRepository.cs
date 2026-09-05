using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class CityRepository(ClinicDbContext dbContext)
    : ClinicRepository<City>(dbContext), ICityRepository
{
    public async Task<List<City>> GetByStateIdAsync(Guid stateId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(c => c.StateId == stateId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsForStateAsync(Guid cityId, Guid stateId, CancellationToken ct = default)
        => await DbSet.AnyAsync(c => c.Id == cityId && c.StateId == stateId, ct);
}
