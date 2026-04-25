using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<IpdRegistration>(dbContext), IIpdRegistrationRepository
{
    public async Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(i => i.Patient)
            .Where(i => i.ApplicationId == applicationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.AdmissionNumber.Contains(search) ||
                i.DoctorName.Contains(search) ||
                i.Patient.FirstName.Contains(search) ||
                i.Patient.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.AdmissionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountTodayAsync(Guid applicationId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.CountAsync(
            i => i.ApplicationId == applicationId && i.AdmissionDate.Date == today, ct);
    }

    public override async Task<IpdRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
}
