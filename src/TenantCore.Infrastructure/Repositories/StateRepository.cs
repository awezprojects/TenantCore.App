using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class StateRepository(ClinicDbContext dbContext)
    : ClinicRepository<State>(dbContext), IStateRepository
{
    public async Task<List<State>> GetAllOrderedAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
}
