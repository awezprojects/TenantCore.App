using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class DosageRemarkRepository(ClinicDbContext dbContext)
    : ClinicRepository<DosageRemark>(dbContext), IDosageRemarkRepository
{
    public async Task<(IEnumerable<DosageRemark> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, MedicineFormType? form,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(d => d.ApplicationId == applicationId);

        if (form.HasValue)
            query = query.Where(d => d.MedicineForm == form.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.MedicineForm).ThenBy(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
