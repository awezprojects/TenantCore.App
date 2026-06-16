using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicUsgTemplateRepository : IRepository<ClinicUsgTemplate>
{
    Task<ClinicUsgTemplate?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<ClinicUsgTemplate?> GetByApplicationIdWithRowsAsync(Guid applicationId, CancellationToken ct = default);
    void RemoveRows(IEnumerable<UsgTemplateRow> rows);
    Task AddRowsAsync(IEnumerable<UsgTemplateRow> rows, CancellationToken ct = default);
}
