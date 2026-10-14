using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

/// <summary>
/// The append-only wallet ledger. There is deliberately no update or delete member — a correction is
/// a new <see cref="WalletReferenceType.Adjustment"/> row.
/// </summary>
public interface IClinicWalletTransactionRepository : IRepository<ClinicWalletTransaction>
{
    /// <summary>Newest-first page of one clinic's ledger, for the wallet screen.</summary>
    Task<IReadOnlyList<ClinicWalletTransaction>> GetForClinicAsync(
        Guid applicationId, int limit, CancellationToken ct = default);

    /// <summary>
    /// The idempotency pre-check: has this reference already moved money? The unique index over
    /// (ApplicationId, ReferenceType, ReferenceId) is the backstop for a genuine race.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid applicationId, WalletReferenceType referenceType, Guid referenceId, CancellationToken ct = default);
}
