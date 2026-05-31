using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class OpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<OpdRegistration>(dbContext), IOpdRegistrationRepository
{
    public async Task<(IEnumerable<OpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search,
        Guid? doctorUserId = null, bool todayOnly = false,
        DateTime? fromDate = null, DateTime? toDate = null,
        OpdStatus? statusFilter = null, bool notVisited = false,
        CancellationToken ct = default)
    {
        var today = DateTime.Now.Date;

        var query = DbSet
            .Include(o => o.Patient)
            .Where(o => o.ApplicationId == applicationId);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.RegistrationNumber.Contains(search) ||
                o.DoctorName.Contains(search) ||
                o.Patient.FirstName.Contains(search) ||
                o.Patient.LastName.Contains(search));

        // Doctor filter
        if (doctorUserId.HasValue)
            query = query.Where(o => o.DoctorUserId == doctorUserId.Value);

        // Date range
        if (todayOnly)
        {
            query = query.Where(o => o.RegistrationDate.Date == today &&
                                     (o.Status == OpdStatus.Pending || o.Status == OpdStatus.InProgress));
        }
        else if (fromDate.HasValue || toDate.HasValue)
        {
            if (fromDate.HasValue)
                query = query.Where(o => o.RegistrationDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(o => o.RegistrationDate.Date <= toDate.Value.Date);
        }
        else
        {
            query = query.Where(o => o.RegistrationDate.Date == today);
        }

        // Status filter — "Not Visited" = past Pending records
        if (notVisited)
        {
            query = query.Where(o => o.Status == OpdStatus.Pending && o.RegistrationDate.Date < today);
        }
        else if (statusFilter.HasValue)
        {
            // "Waiting" tab = today's Pending (past Pending are Not Visited)
            if (statusFilter.Value == OpdStatus.Pending)
                query = query.Where(o => o.Status == OpdStatus.Pending && o.RegistrationDate.Date >= today);
            else
                query = query.Where(o => o.Status == statusFilter.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountTodayAsync(Guid applicationId, CancellationToken ct = default)
    {
        var today = DateTime.Now.Date;
        return await DbSet.CountAsync(
            o => o.ApplicationId == applicationId && o.RegistrationDate.Date == today, ct);
    }

    public async Task<int> CountTodayByDoctorAsync(Guid applicationId, Guid doctorUserId, CancellationToken ct = default)
    {
        var today = DateTime.Now.Date;
        return await DbSet.CountAsync(
            o => o.ApplicationId == applicationId &&
                 o.DoctorUserId == doctorUserId &&
                 o.RegistrationDate.Date == today &&
                 (o.Status == OpdStatus.Pending || o.Status == OpdStatus.InProgress), ct);
    }

    public override async Task<OpdRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
