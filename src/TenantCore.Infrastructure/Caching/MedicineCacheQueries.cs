using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Caching;

/// <summary>
/// The full-table reads used to build each medicine-related cache snapshot. Shared between
/// <see cref="MedicineCacheWarmupService"/> (the normal path) and the cached repositories'
/// cold-start fallback (serving a single request live if it arrives before the first
/// background refresh has completed).
/// </summary>
internal static class MedicineCacheQueries
{
    public static Task<List<Medicine>> LoadSystemMedicinesAsync(ClinicDbContext dbContext, CancellationToken ct) =>
        dbContext.Set<Medicine>()
            .AsNoTracking()
            .Include(m => m.MedicineType)
            .Include(m => m.DosageForm)
            .Where(m => m.ApplicationId == null)
            .ToListAsync(ct);

    public static Task<List<MedicineType>> LoadMedicineTypesAsync(ClinicDbContext dbContext, CancellationToken ct) =>
        dbContext.Set<MedicineType>().AsNoTracking().ToListAsync(ct);

    public static Task<List<MedicineDosageForm>> LoadMedicineDosageFormsAsync(ClinicDbContext dbContext, CancellationToken ct) =>
        dbContext.Set<MedicineDosageForm>().AsNoTracking().ToListAsync(ct);
}
