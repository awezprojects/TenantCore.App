using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class VitalPresetLookupItemRepository(ClinicDbContext dbContext)
    : ClinicRepository<VitalPresetLookupItem>(dbContext), IVitalPresetLookupItemRepository
{
    public async Task<List<VitalPresetLookupItem>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(v => v.ApplicationId == null || v.ApplicationId == applicationId)
            .OrderBy(v => v.VitalField).ThenBy(v => v.Value)
            .ToListAsync(ct);

    public async Task<VitalPresetLookupItem?> FindAsync(VitalFieldType vitalField, Guid applicationId, string value, CancellationToken ct = default)
        => await DbSet
            .Where(v => v.VitalField == vitalField && (v.ApplicationId == null || v.ApplicationId == applicationId))
            .FirstOrDefaultAsync(v => v.Value.ToLower() == value.ToLower(), ct);
}
