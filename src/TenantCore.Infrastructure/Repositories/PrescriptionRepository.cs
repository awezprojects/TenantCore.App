using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class PrescriptionRepository(ClinicDbContext dbContext)
    : ClinicRepository<Prescription>(dbContext), IPrescriptionRepository
{
    public async Task<(IEnumerable<Prescription> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search,
        Guid? doctorUserId, Guid? patientId, DateTime? from, DateTime? to,
        CancellationToken ct = default)
    {
        var query = DbSet
            .Include(p => p.Items)
            .Where(p => p.ApplicationId == applicationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.PrescriptionNumber.Contains(search) ||
                p.DoctorName.Contains(search));

        if (doctorUserId.HasValue)
            query = query.Where(p => p.DoctorUserId == doctorUserId.Value);

        if (patientId.HasValue)
            query = query.Where(p => p.PatientId == patientId.Value);

        if (from.HasValue)
            query = query.Where(p => p.PrescribedDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PrescribedDate <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PrescribedDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Prescription?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(p => p.Items)
            .Include(p => p.Reports)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prescription?> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, CancellationToken ct = default)
        => await DbSet
            .Include(p => p.Items)
            .Include(p => p.Reports)
            .FirstOrDefaultAsync(p => p.OpdRegistrationId == opdRegistrationId, ct);

    public async Task<int> CountTodayByApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var today = DateTime.Now.Date;
        return await DbSet.CountAsync(
            p => p.ApplicationId == applicationId && p.PrescribedDate.Date == today, ct);
    }
}
