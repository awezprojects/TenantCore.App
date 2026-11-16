using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdBedTransferHistoryRepository(ClinicDbContext dbContext)
    : ClinicRepository<IpdBedTransferHistory>(dbContext), IIpdBedTransferHistoryRepository
{
    public async Task<IpdBedTransferHistory?> GetCurrentSegmentAsync(
        Guid ipdRegistrationId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(h =>
                h.IpdRegistrationId == ipdRegistrationId &&
                h.ApplicationId == applicationId &&
                h.OccupiedTo == null,
            cancellationToken);

    public async Task<IEnumerable<IpdBedTransferHistory>> GetByRegistrationAsync(
        Guid ipdRegistrationId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(h => h.Bed)
            .Include(h => h.Room)
            .Include(h => h.Ward)
            .AsNoTracking()
            .Where(h => h.IpdRegistrationId == ipdRegistrationId && h.ApplicationId == applicationId)
            .OrderBy(h => h.OccupiedFrom)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasAnyForBedAsync(Guid bedId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().AnyAsync(h => h.BedId == bedId && h.ApplicationId == applicationId, cancellationToken);

    public async Task<bool> HasAnyForRoomAsync(Guid roomId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().AnyAsync(h => h.RoomId == roomId && h.ApplicationId == applicationId, cancellationToken);

    public async Task<bool> HasAnyForWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().AnyAsync(h => h.WardId == wardId && h.ApplicationId == applicationId, cancellationToken);
}
