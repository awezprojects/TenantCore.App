using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicFeatureFlagsRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicFeatureFlags>(dbContext), IClinicFeatureFlagsRepository
{
    public async Task<ClinicFeatureFlags?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(f => f.ApplicationId == applicationId, ct);
}
