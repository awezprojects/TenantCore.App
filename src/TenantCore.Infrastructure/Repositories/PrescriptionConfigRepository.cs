using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class PrescriptionConfigRepository(ClinicDbContext dbContext)
    : ClinicRepository<PrescriptionConfig>(dbContext), IPrescriptionConfigRepository
{
    public async Task<PrescriptionConfig?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(c => c.ApplicationId == applicationId, ct);
}
