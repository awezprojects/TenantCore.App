using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class OpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<OpdRegistration>(dbContext), IOpdRegistrationRepository
{
    public async Task<(IEnumerable<OpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(o => o.Patient)
            .Where(o => o.ApplicationId == applicationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.RegistrationNumber.Contains(search) ||
                o.DoctorName.Contains(search) ||
                o.Patient.FirstName.Contains(search) ||
                o.Patient.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.RegistrationDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountTodayAsync(Guid applicationId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.CountAsync(
            o => o.ApplicationId == applicationId && o.RegistrationDate.Date == today, ct);
    }

    public override async Task<OpdRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
