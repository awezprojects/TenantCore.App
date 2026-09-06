using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IMedicineBundleRepository : IClinicRepository<MedicineBundle>
{
    Task<IReadOnlyList<MedicineBundle>> GetAllForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task<MedicineBundle?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
}
