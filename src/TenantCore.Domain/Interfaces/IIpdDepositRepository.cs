using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdDepositRepository : IClinicRepository<IpdDeposit>
{
    Task<IEnumerable<IpdDeposit>> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<IpdDeposit>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default);
}
