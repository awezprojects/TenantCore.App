using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicAccountRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicAccount>(dbContext), IClinicAccountRepository
{
    public async Task<ClinicAccount?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);

    public async Task<bool> IsSuspendedAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(a => a.ApplicationId == applicationId
                        && a.AccessStatus == ClinicAccessStatus.Suspended, ct);
}
