using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class PatientRepository(ClinicDbContext dbContext)
    : ClinicRepository<Patient>(dbContext), IPatientRepository
{
    public async Task<(IEnumerable<Patient> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet.Where(p => p.ApplicationId == applicationId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.FirstName.Contains(search) ||
                p.LastName.Contains(search) ||
                p.PhoneNumber.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Patient?> GetByPhoneAsync(Guid applicationId, string phone, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.ApplicationId == applicationId && p.PhoneNumber == phone, ct);
}
