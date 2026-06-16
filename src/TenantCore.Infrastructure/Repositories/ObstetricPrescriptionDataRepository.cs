using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ObstetricPrescriptionDataRepository(ClinicDbContext context) : IObstetricPrescriptionDataRepository
{
    public async Task<ObstetricPrescriptionData?> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default)
        => await context.ObstetricPrescriptionData
            .FirstOrDefaultAsync(o => o.PrescriptionId == prescriptionId, ct);

    public async Task<ObstetricPrescriptionData?> GetMostRecentWithLmpByPatientIdAsync(Guid patientId, Guid applicationId, CancellationToken ct = default)
        => await (from opd in context.ObstetricPrescriptionData
                  join p in context.Prescriptions on opd.PrescriptionId equals p.Id
                  where p.PatientId == patientId
                        && p.ApplicationId == applicationId
                        && opd.Lmp != null
                  orderby p.CreatedAt descending
                  select opd)
               .FirstOrDefaultAsync(ct);

    public async Task AddAsync(ObstetricPrescriptionData data, CancellationToken ct = default)
        => await context.ObstetricPrescriptionData.AddAsync(data, ct);

    public void Update(ObstetricPrescriptionData data)
        => context.ObstetricPrescriptionData.Update(data);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
