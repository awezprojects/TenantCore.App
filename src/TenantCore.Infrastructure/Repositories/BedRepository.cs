using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class BedRepository(ClinicDbContext dbContext) : ClinicRepository<Bed>(dbContext), IBedRepository
{
    public async Task<IEnumerable<Bed>> GetByRoomAsync(Guid roomId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.Ward)
            .Include(b => b.Room)
            .Where(b => b.RoomId == roomId && b.ApplicationId == applicationId)
            .OrderBy(b => b.BedNumber)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bed>> GetAvailableByWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.Ward)
            .Include(b => b.Room)
            .Where(b => b.WardId == wardId && b.ApplicationId == applicationId && !b.IsOccupied && b.IsActive)
            .OrderBy(b => b.BedNumber)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bed>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.Ward)
            .Include(b => b.Room)
            .Where(b => b.ApplicationId == applicationId)
            .OrderBy(b => b.BedNumber)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNumberAsync(Guid roomId, string bedNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(b => b.RoomId == roomId && b.BedNumber == bedNumber);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Bed?> FindByLocationAsync(Guid applicationId, string wardName, string roomNumber, string bedNumber, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.Ward)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b =>
                b.ApplicationId == applicationId &&
                b.Ward.Name == wardName &&
                b.Room.RoomNumber == roomNumber &&
                b.BedNumber == bedNumber,
            cancellationToken);
}
