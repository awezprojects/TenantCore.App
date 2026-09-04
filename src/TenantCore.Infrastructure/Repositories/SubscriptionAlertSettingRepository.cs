using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class SubscriptionAlertSettingRepository(ClinicDbContext dbContext)
    : ClinicRepository<SubscriptionAlertSetting>(dbContext), ISubscriptionAlertSettingRepository
{
    public async Task<IReadOnlyList<SubscriptionAlertSetting>> GetAllOrderedAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);
}
