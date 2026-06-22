using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class DoctorFeeConfigRepository(ClinicDbContext dbContext) : ClinicRepository<DoctorFeeConfig>(dbContext), IDoctorFeeConfigRepository
{
    public async Task<DoctorFeeConfig?> GetByDoctorProfileIdAsync(Guid doctorProfileId, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorProfileId == doctorProfileId && d.ApplicationId == applicationId, ct);
}
