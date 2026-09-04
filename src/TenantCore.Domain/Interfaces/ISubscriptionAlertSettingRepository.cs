using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

/// <summary>Global configuration reads — not tenant-scoped. Consumed today only by the admin-facing settings endpoints; see SubscriptionAlertSetting's XML doc.</summary>
public interface ISubscriptionAlertSettingRepository : IClinicRepository<SubscriptionAlertSetting>
{
    Task<IReadOnlyList<SubscriptionAlertSetting>> GetAllOrderedAsync(CancellationToken ct = default);
}
