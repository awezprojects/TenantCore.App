using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicAccountRepository : IRepository<ClinicAccount>
{
    /// <summary>Tracked — callers mutate the row. Null when the clinic has never needed one (it is then Active with the public catalogue).</summary>
    Task<ClinicAccount?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>No-tracking single-column check used by the request guard on every clinic-scoped call.</summary>
    Task<bool> IsSuspendedAsync(Guid applicationId, CancellationToken ct = default);
}
