using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdRegistrationRepository : IClinicRepository<IpdRegistration>
{
    Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default);

    Task<int> CountTodayAsync(Guid applicationId, CancellationToken ct = default);
}
