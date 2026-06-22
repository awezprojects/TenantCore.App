using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class CounterSessionRepository(ClinicDbContext dbContext) : ClinicRepository<CounterSession>(dbContext), ICounterSessionRepository
{
    public async Task<CounterSession?> GetActiveSessionAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.Status == CounterSessionStatus.Open, ct);

    public async Task<IReadOnlyList<CounterSession>> GetAllActiveAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(s => s.ApplicationId == applicationId && s.Status == CounterSessionStatus.Open)
            .OrderBy(s => s.OpenedAt)
            .ToListAsync(ct);

    public async Task<CounterSession?> GetByDateAsync(DateTime date, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.SessionDate.Date == date.Date, ct);
}
