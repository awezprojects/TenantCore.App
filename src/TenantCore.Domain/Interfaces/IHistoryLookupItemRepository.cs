using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IHistoryLookupItemRepository : IRepository<HistoryLookupItem>
{
    // Global (ApplicationId == null) items plus this clinic's own custom additions.
    Task<List<HistoryLookupItem>> GetForApplicationAsync(Guid applicationId, CancellationToken ct = default);

    Task<HistoryLookupItem?> FindAsync(HistoryItemType type, Guid applicationId, string value, CancellationToken ct = default);
}
