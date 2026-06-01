using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class WardRepository(ClinicDbContext dbContext) : ClinicRepository<Ward>(dbContext), IWardRepository
{
    public async Task<IEnumerable<Ward>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .Where(w => w.ApplicationId == applicationId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

    public async Task<Ward?> GetByIdWithRoomsAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .FirstOrDefaultAsync(w => w.Id == id && w.ApplicationId == applicationId, cancellationToken);

    public async Task<bool> ExistsByNameAsync(Guid applicationId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(w => w.ApplicationId == applicationId && w.Name == name);
        if (excludeId.HasValue)
            query = query.Where(w => w.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
