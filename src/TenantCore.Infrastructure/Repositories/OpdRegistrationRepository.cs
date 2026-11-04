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

        // "Not Visited" overrides all date/status filters — past pending only
        if (notVisited)
        {
            query = query.Where(o => o.Status == OpdStatus.Pending && o.RegistrationDate.Date < today);
        }
        else
        {
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

            // Status filter
            if (statusFilter.HasValue)
            {
                // "Waiting" tab = today's Pending (past Pending are Not Visited)
                if (statusFilter.Value == OpdStatus.Pending)
                    query = query.Where(o => o.Status == OpdStatus.Pending && o.RegistrationDate.Date >= today);
                else
                    query = query.Where(o => o.Status == statusFilter.Value);
            }
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // DEF-OPD-13: deriving the next number from the highest *existing* row for the day reused a
    // token once its registration was hard-deleted. A dedicated counter, never decremented, fixes
    // that — staged here, not saved; it travels in the parent handler's own SaveChangesAsync so the
    // registration and its number commit together.
    public async Task<string> GetNextRegistrationNumberAsync(Guid applicationId, CancellationToken ct = default)
    {
        var today = DateTime.Now.Date;
        var prefix = $"OPD-{today:yyyyMMdd}-";

        var counters = DbContext.Set<OpdRegistrationNumberCounter>();
        var counter = await counters.FirstOrDefaultAsync(
            c => c.ApplicationId == applicationId && c.CounterDate == today, ct);

        if (counter is null)
        {
            // DEF-OPD-23: seed from today's highest existing registration number (not just rows
            // deleted since this table existed — any registration already on record for the day,
            // deleted or not, since the sequence portion is all that matters here) so a counter
            // created on a day that already has registrations cannot reissue a number.
            var lastExisting = await DbSet
                .Where(o => o.ApplicationId == applicationId && o.RegistrationDate.Date == today)
                .OrderByDescending(o => o.RegistrationNumber)
                .Select(o => o.RegistrationNumber)
                .FirstOrDefaultAsync(ct);

            var seedSequence = 0;
            if (lastExisting is not null && lastExisting.Length > prefix.Length &&
                int.TryParse(lastExisting[prefix.Length..], out var lastSequence))
            {
                seedSequence = lastSequence;
            }

            counter = OpdRegistrationNumberCounter.CreateSeeded(applicationId, today, seedSequence);
            await counters.AddAsync(counter, ct);
        }

        var nextSequence = counter.Next();
        return $"{prefix}{nextSequence:D4}";
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

    public async Task<IEnumerable<OpdRegistration>> GetByIdsAsync(IEnumerable<Guid> ids, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Include(o => o.Patient)
            .Where(o => o.ApplicationId == applicationId && ids.Contains(o.Id))
            .ToListAsync(ct);

    public async Task<bool> ExistsForPatientDoctorOnDateAsync(
        Guid applicationId, Guid patientId, Guid doctorUserId, DateTime date, CancellationToken ct = default)
        => await DbSet.AnyAsync(o =>
            o.ApplicationId == applicationId &&
            o.PatientId == patientId &&
            o.DoctorUserId == doctorUserId &&
            o.RegistrationDate.Date == date.Date &&
            o.Status != OpdStatus.Cancelled, ct);
}
