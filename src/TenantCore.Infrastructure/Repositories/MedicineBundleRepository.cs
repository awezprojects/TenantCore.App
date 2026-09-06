using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class MedicineBundleRepository(ClinicDbContext dbContext)
    : ClinicRepository<MedicineBundle>(dbContext), IMedicineBundleRepository
{
    public async Task<IReadOnlyList<MedicineBundle>> GetAllForApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Include(b => b.Items)
            .Where(b => b.ApplicationId == applicationId)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

    public async Task<MedicineBundle?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
}
