using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IMedicineDosageFormRepository : IClinicRepository<MedicineDosageForm>
{
    Task<(IEnumerable<MedicineDosageForm> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<MedicineDosageForm?> GetByNameAsync(string name, CancellationToken ct = default);
}
