using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class RoomRepository(ClinicDbContext dbContext) : ClinicRepository<Room>(dbContext), IRoomRepository
{
    public async Task<IEnumerable<Room>> GetByWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(r => r.Ward)
            .Include(r => r.Beds)
            .Where(r => r.WardId == wardId && r.ApplicationId == applicationId)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync(cancellationToken);

    public async Task<Room?> GetByIdWithBedsAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(r => r.Ward)
            .Include(r => r.Beds)
            .FirstOrDefaultAsync(r => r.Id == id && r.ApplicationId == applicationId, cancellationToken);

    public async Task<bool> ExistsByNumberAsync(Guid wardId, string roomNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(r => r.WardId == wardId && r.RoomNumber == roomNumber);
        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
