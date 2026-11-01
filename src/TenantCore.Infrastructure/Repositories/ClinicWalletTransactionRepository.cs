using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicWalletTransactionRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicWalletTransaction>(dbContext), IClinicWalletTransactionRepository
{
    public async Task<IReadOnlyList<ClinicWalletTransaction>> GetForClinicAsync(
        Guid applicationId, int limit, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(t => t.ApplicationId == applicationId)
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        Guid applicationId, WalletReferenceType referenceType, Guid referenceId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .AnyAsync(t => t.ApplicationId == applicationId
                        && t.ReferenceType == referenceType
                        && t.ReferenceId == referenceId, ct);
}
