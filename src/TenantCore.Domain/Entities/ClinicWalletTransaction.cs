using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

/// <summary>
/// The append-only wallet ledger. Tenant-scoped. One row per balance movement, written in the
/// same SaveChanges as the movement itself, so "where did this balance come from and where did it
/// go" is answerable without replaying anything else.
///
/// There is deliberately no update or delete path anywhere: a correction is a new
/// <see cref="WalletReferenceType.Adjustment"/> row. This is what makes the ledger trustworthy.
/// </summary>
public class ClinicWalletTransaction : AuditableEntity
{
    public Guid ApplicationId { get; private set; }

    /// <summary>Credit or Debit. <see cref="Amount"/> is always positive — this carries the sign.</summary>
    public WalletTransactionType Type { get; private set; }

    /// <summary>Always positive. The sign lives on <see cref="Type"/>.</summary>
    public decimal Amount { get; private set; }

    /// <summary>The wallet balance immediately after this row, so the ledger reads top-to-bottom.</summary>
    public decimal BalanceAfter { get; private set; }

    public WalletReferenceType ReferenceType { get; private set; }

    /// <summary>
    /// Top-up → the paid <c>SubscriptionPayment.Id</c>; usage → the <c>ClinicUsageRecord.Id</c>;
    /// admin credit → the admin-supplied idempotency key; adjustment → free-form.
    /// The unique index over (ApplicationId, ReferenceType, ReferenceId) makes a replay unable to
    /// move money twice.
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>System-generated only — never a patient name, never free text from a user.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>UTC. The moment the movement happened, not the moment the row was inserted.</summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>The acting clinic user, when there is one.</summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>Set only for an <see cref="WalletReferenceType.AdminCredit"/>.</summary>
    public string? CreatedByAdminEmail { get; private set; }

    private ClinicWalletTransaction() { }

    private static ClinicWalletTransaction Create(
        Guid applicationId, WalletTransactionType type, decimal amount, decimal balanceAfter,
        WalletReferenceType referenceType, Guid? referenceId, string description, DateTime occurredAt,
        Guid? createdByUserId, string? createdByAdminEmail) => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Type = type,
            Amount = amount,
            BalanceAfter = balanceAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = description,
            OccurredAt = occurredAt,
            CreatedByUserId = createdByUserId,
            CreatedByAdminEmail = createdByAdminEmail,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>A credit against a wallet top-up settled by Razorpay.</summary>
    public static ClinicWalletTransaction CreateTopUpCredit(
        Guid applicationId, decimal amount, decimal balanceAfter, Guid topUpPaymentId,
        string description, DateTime occurredAt, Guid? createdByUserId = null)
        => Create(applicationId, WalletTransactionType.Credit, amount, balanceAfter,
            WalletReferenceType.TopUpPayment, topUpPaymentId, description, occurredAt, createdByUserId, null);

    /// <summary>A debit for a metered operation. Paired with exactly one <see cref="ClinicUsageRecord"/>.</summary>
    public static ClinicWalletTransaction CreateUsageDebit(
        Guid applicationId, decimal amount, decimal balanceAfter, Guid usageRecordId,
        string description, DateTime occurredAt, Guid? createdByUserId = null)
        => Create(applicationId, WalletTransactionType.Debit, amount, balanceAfter,
            WalletReferenceType.Usage, usageRecordId, description, occurredAt, createdByUserId, null);

    /// <summary>
    /// A direct credit by an internal admin. <paramref name="idempotencyKey"/> is supplied by the
    /// Admin portal so a retried call cannot credit twice.
    /// </summary>
    public static ClinicWalletTransaction CreateAdminCredit(
        Guid applicationId, decimal amount, decimal balanceAfter, Guid idempotencyKey,
        string description, DateTime occurredAt, string adminEmail)
        => Create(applicationId, WalletTransactionType.Credit, amount, balanceAfter,
            WalletReferenceType.AdminCredit, idempotencyKey, description, occurredAt, null, adminEmail);

    /// <summary>
    /// A manual correction. Never edits an earlier row — it is a new movement that references it.
    /// </summary>
    public static ClinicWalletTransaction CreateAdjustment(
        Guid applicationId, WalletTransactionType type, decimal amount, decimal balanceAfter,
        Guid? referenceId, string description, DateTime occurredAt, string adminEmail)
        => Create(applicationId, type, amount, balanceAfter,
            WalletReferenceType.Adjustment, referenceId, description, occurredAt, null, adminEmail);
}
