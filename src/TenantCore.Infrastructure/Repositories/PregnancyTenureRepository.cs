using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class PregnancyTenureRepository(ClinicDbContext dbContext)
    : ClinicRepository<PregnancyTenure>(dbContext), IPregnancyTenureRepository
{
    public async Task<PregnancyTenure?> GetActiveForPatientAsync(
        Guid patientId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.PatientId == patientId
                     && t.ApplicationId == applicationId
                     && t.Status == PregnancyTenureStatus.Active,
                ct);

    public async Task<IEnumerable<PregnancyTenure>> GetAllOverdueAsync(
        Guid applicationId, DateOnly today, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Include(t => t.Patient)
            .Where(t => t.ApplicationId == applicationId
                        && t.Status == PregnancyTenureStatus.Active
                        && (t.EddByUsg != null ? t.EddByUsg < today : t.EddByLmp < today))
            .OrderBy(t => t.EddByUsg != null ? t.EddByUsg : t.EddByLmp)
            .ToListAsync(ct);

    public async Task<IEnumerable<PregnancyTenure>> GetAllForPatientAsync(
        Guid patientId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && t.ApplicationId == applicationId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, bool>> GetTenureInfoForPatientsAsync(
        IEnumerable<Guid> patientIds, Guid applicationId, CancellationToken ct = default)
    {
        var idList = patientIds as IList<Guid> ?? patientIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, bool>();

        var rows = await DbSet
            .AsNoTracking()
            .Where(t => idList.Contains(t.PatientId) && t.ApplicationId == applicationId)
            .Select(t => new { t.PatientId, t.Status })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.PatientId)
            .ToDictionary(
                g => g.Key,
                g => g.Any(r => r.Status == PregnancyTenureStatus.Active));
    }
}
