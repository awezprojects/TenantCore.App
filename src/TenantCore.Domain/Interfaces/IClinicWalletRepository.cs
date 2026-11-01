using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

/// <summary>
/// The clinic's prepaid wallet. <c>AddAsync</c> is inherited from <see cref="IRepository{T}"/>.
/// </summary>
public interface IClinicWalletRepository : IRepository<ClinicWallet>
{
    /// <summary>
    /// Tracked — the debit path needs the change tracker <i>and</i> the RowVersion token, because the
    /// balance check and the write must be one atomic unit.
    /// </summary>
    Task<ClinicWallet?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>No-tracking read for the wallet screen, which only displays.</summary>
    Task<ClinicWallet?> GetByApplicationIdNoTrackingAsync(Guid applicationId, CancellationToken ct = default);
}
