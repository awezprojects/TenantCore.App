using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;

namespace TenantCore.Domain.Entities;

/// <summary>
/// A clinic's prepaid pay-as-you-go wallet. Tenant-scoped, at most one row per clinic.
///
/// <see cref="Balance"/> only ever moves through <see cref="Credit"/> and <see cref="Debit"/>, each
/// of which is paired with exactly one <see cref="ClinicWalletTransaction"/> staged in the same unit
/// of work — there is deliberately no <c>SetBalance</c>. The inherited RowVersion column is the only
/// thing standing between two concurrent charges and a negative balance.
/// </summary>
public class ClinicWallet : AuditableEntity
{
    public Guid ApplicationId { get; private set; }

    public decimal Balance { get; private set; }

    /// <summary>Purely informational — it drives the client's header chip and the Admin low-balance list. Never blocks anything.</summary>
    public decimal LowBalanceThreshold { get; private set; }

    public string Currency { get; private set; } = "INR";

    public DateTime? LastTopUpAt { get; private set; }

    public DateTime? LastDebitAt { get; private set; }

    private ClinicWallet() { }

    public static ClinicWallet Create(Guid applicationId, decimal lowBalanceThreshold, string currency)
        => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Balance = 0m,
            LowBalanceThreshold = lowBalanceThreshold,
            Currency = currency,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>At or below the threshold. A warning only — it must never block an operation.</summary>
    public bool IsLow => Balance <= LowBalanceThreshold;

    public bool HasFunds(decimal amount) => Balance >= amount;

    /// <summary>Adds funds. The caller stages the matching credit ledger row in the same SaveChanges.</summary>
    public void Credit(decimal amount, DateTime utcNow)
    {
        if (amount <= 0m)
            throw new DomainException("Invalid wallet amount.");

        Balance += amount;
        LastTopUpAt = utcNow;
        SetUpdatedAt();
    }

    /// <summary>
    /// Removes funds for a metered operation. The caller stages the matching debit ledger row in the
    /// same SaveChanges. Both throws stay HTTP-free — the handler converts the insufficient-funds
    /// case into a 403 <c>insufficient_wallet_balance</c> (ADR-012 decision 12).
    /// </summary>
    public void Debit(decimal amount, DateTime utcNow)
    {
        if (amount <= 0m)
            throw new DomainException("Invalid wallet amount.");

        if (!HasFunds(amount))
            throw new DomainException("Insufficient wallet balance.");

        Balance -= amount;
        LastDebitAt = utcNow;
        SetUpdatedAt();
    }
}
