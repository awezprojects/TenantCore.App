using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IVitalPresetLookupItemRepository : IRepository<VitalPresetLookupItem>
{
    // Global (ApplicationId == null) items plus this clinic's own custom additions.
    Task<List<VitalPresetLookupItem>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default);

    Task<VitalPresetLookupItem?> FindAsync(VitalFieldType vitalField, Guid applicationId, string value, CancellationToken ct = default);
}
