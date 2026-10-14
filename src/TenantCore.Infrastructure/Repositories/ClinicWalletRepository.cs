using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicWalletRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicWallet>(dbContext), IClinicWalletRepository
{
    /// <summary>Tracked — the debit path needs the change tracker and the RowVersion concurrency token.</summary>
    public async Task<ClinicWallet?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(w => w.ApplicationId == applicationId, ct);

    public async Task<ClinicWallet?> GetByApplicationIdNoTrackingAsync(Guid applicationId, CancellationToken ct = default)
        => await DbSet.AsNoTracking().FirstOrDefaultAsync(w => w.ApplicationId == applicationId, ct);
}
