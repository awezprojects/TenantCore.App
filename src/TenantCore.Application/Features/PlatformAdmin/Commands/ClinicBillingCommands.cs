using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PlatformAdmin.Commands;

/// <summary>
/// Internal admin switching a clinic's billing mode, and optionally setting its per-clinic
/// registration fee and free test days.
///
/// <paramref name="ApplyNow"/> true applies immediately and clears any queued change; false queues the
/// change to the end of the clinic's current coverage, so days it has already paid for are never lost.
/// A different mode cannot be queued while one is unresolved — <c>billing_mode_locked</c>.
/// </summary>
public sealed record SetClinicBillingCommand(
    Guid ApplicationId,
    BillingMode BillingMode,
    bool ApplyNow,
    decimal? RegistrationFee,
    int? FreeTestDays,
    string? Reason,
    Guid AdminUserId,
    string AdminEmail,
    /// <summary>
    /// Credit to store as <c>PendingWalletCreditOnFeePayment</c> — only used at activation time
    /// for PAYG clinics whose registration fee must be paid before the wallet unlocks. Null
    /// (the default) leaves the existing pending credit unchanged.
    /// </summary>
    decimal? InitialWalletCreditOnFeePayment = null)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Billing Mode Set";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; mode={BillingMode}; queued={(!ApplyNow).ToString().ToLowerInvariant()}; "
        + $"freeTestDays={(FreeTestDays?.ToString() ?? "-")}; admin={AdminEmail}";
}

/// <summary>One operation's price. Zero means free, which is a legitimate thing to set.</summary>
public sealed record UsageRateSetting(BillingItem BillingItem, decimal Rate);

/// <summary>
/// Internal admin setting this clinic's rates. The clinic's own price always beats the catalogue, so
/// setting a rate here is how a negotiated price is honoured. Setting a rate equal to the current one
/// changes nothing, and the rate history is preserved rather than overwritten.
/// </summary>
public sealed record SetClinicUsageRatesCommand(
    Guid ApplicationId,
    IReadOnlyList<UsageRateSetting> Rates,
    string? Reason,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Usage Rates Set";

    /// <summary>Names only, never a body: the rates themselves are in the audit row written by the handler.</summary>
    public string ActionLogContext =>
        $"applicationId={ApplicationId}; rates={string.Join(",", Rates.Select(r => r.BillingItem))}; admin={AdminEmail}";
}

/// <summary>
/// Internal admin sending a clinic a top-up link for an amount <b>they</b> chose. Nothing is credited
/// by this command — the clinic still has to pay the link and have the gateway confirm it. Passing
/// <paramref name="ReferenceId"/> makes a retried call return the link that already exists instead of
/// opening a second one.
/// </summary>
public sealed record SendClinicWalletTopUpCommand(
    Guid ApplicationId,
    decimal Amount,
    string Reason,
    Guid? ReferenceId,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Wallet Top-Up Sent";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; amount={Amount}; referenceId={(ReferenceId?.ToString() ?? "-")}; admin={AdminEmail}";
}

/// <summary>
/// Internal admin crediting a clinic's wallet directly, without a payment — a goodwill credit or a
/// compensation. <paramref name="ReferenceId"/> is the idempotency key: replaying the same one is a
/// no-op that returns the current balance, so a retried call cannot move money twice.
/// </summary>
public sealed record CreditClinicWalletCommand(
    Guid ApplicationId,
    decimal Amount,
    string Reason,
    Guid ReferenceId,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest<decimal>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Wallet Credited";

    public string ActionLogContext =>
        $"applicationId={ApplicationId}; amount={Amount}; referenceId={ReferenceId}; admin={AdminEmail}";
}

/// <summary>
/// Internal admin sending (or re-sending) the clinic's registration fee payment link. If an open link
/// for the same fee amount already exists it is returned as-is (idempotent). If the last link has
/// expired a new one is created and the old one is superseded.
/// </summary>
public sealed record AdminSendRegistrationFeeLinkCommand(
    Guid ApplicationId,
    Guid AdminUserId,
    string AdminEmail,
    ClinicContact? Contact)
    : IRequest<Guid>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Registration Fee Link Sent";

    public string ActionLogContext => $"applicationId={ApplicationId}; admin={AdminEmail}";
}

/// <summary>
/// Internal admin manually triggering a Razorpay status check on a clinic's registration fee
/// payment. If the link is already confirmed Paid by the gateway, re-enqueues
/// <c>RecordRegistrationFee</c> so any pending wallet credit that was skipped (e.g. due to a
/// transient validation error on a previous run) is applied. If the link is still open
/// (<c>LinkCreated</c>), enqueues <c>ConfirmPayment</c> to ask Razorpay right now.
/// </summary>
public sealed record AdminCheckRegistrationFeePaymentCommand(
    Guid ApplicationId,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Admin Registration Fee Payment Check";

    public string ActionLogContext => $"applicationId={ApplicationId}; admin={AdminEmail}";
}

/// <summary>
/// Internal admin waiving the one-time registration fee — the clinic will never be asked for it. A
/// fee that is already paid cannot be waived: that would need a refund, not a flag.
/// </summary>
public sealed record WaiveRegistrationFeeCommand(
    Guid ApplicationId,
    string Reason,
    Guid AdminUserId,
    string AdminEmail)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Clinic Registration Fee Waived";

    public string ActionLogContext => $"applicationId={ApplicationId}; admin={AdminEmail}";
}
