using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class DoctorProfileRepository(ClinicDbContext dbContext)
    : ClinicRepository<DoctorProfile>(dbContext), IDoctorProfileRepository
{
    public async Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await DbSet
            .Include(dp => dp.Speciality)
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

    public async Task<IEnumerable<DoctorProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(dp => userIds.Contains(dp.UserId))
            .ToListAsync(ct);

    public async Task<IEnumerable<DoctorProfile>> GetByIdsAsync(IEnumerable<Guid> profileIds, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(dp => profileIds.Contains(dp.Id))
            .ToListAsync(ct);
}
