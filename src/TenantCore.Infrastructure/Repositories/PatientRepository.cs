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

        // DEF-MR-10: a single-field Contains() never matches a multi-word query that spans both
        // FirstName and LastName (e.g. "Dummy Patient 001" when FirstName="Dummy",
        // LastName="Patient 001") — each word must independently match somewhere across the full
        // name or the phone number.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var words = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var word in words)
                query = query.Where(p =>
                    p.FirstName.Contains(word) ||
                    p.LastName.Contains(word) ||
                    p.PhoneNumber.Contains(word));
        }

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
