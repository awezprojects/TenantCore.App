using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicOnboardingRequestRepository(ClinicDbContext dbContext)
    : ClinicRepository<ClinicOnboardingRequest>(dbContext), IClinicOnboardingRequestRepository
{
    private static readonly ClinicOnboardingStatus[] OpenStatuses =
    [
        ClinicOnboardingStatus.Submitted,
        ClinicOnboardingStatus.Approved,
        ClinicOnboardingStatus.AwaitingPayment,
        ClinicOnboardingStatus.PaymentReceived,
        ClinicOnboardingStatus.Provisioning
    ];

    public async Task<ClinicOnboardingRequest?> GetOpenForUserAsync(Guid userId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(r => r.RequestedByUserId == userId && OpenStatuses.Contains(r.Status))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ClinicOnboardingRequest>> GetForUserAsync(Guid userId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

    public async Task<ClinicOnboardingRequest?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(r => r.Id == id && r.RequestedByUserId == userId, ct);

    public async Task<IReadOnlyList<ClinicOnboardingRequest>> GetStuckAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(r => OpenStatuses.Contains(r.Status) && r.Status != ClinicOnboardingStatus.Submitted)
            .ToListAsync(ct);
}
