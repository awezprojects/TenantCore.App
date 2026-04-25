using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicFeeConfigRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicFeeConfig>(dbContext), IClinicFeeConfigRepository
{
    public async Task<ClinicFeeConfig?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(c => c.ApplicationId == applicationId, ct);
}
