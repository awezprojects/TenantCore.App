namespace TenantCore.Application.Common;

/// <summary>
/// Optional: gives a command a human-readable name in the action log, e.g. "Patient Registration".
/// Action logging does NOT depend on this interface any more — every MediatR request whose type
/// name ends in "Command" is logged by <see cref="Behaviors.ActionLoggingBehavior{TRequest,TResponse}"/>
/// (ADR-011: logging is on by default). Without this interface the name is derived from the type
/// ("CreatePatientCommand" → "Create Patient").
/// </summary>
public interface IBusinessAction
{
    /// <summary>Human-readable action name written to the audit trail, e.g. "Patient Registration".</summary>
    string ActionName { get; }
}

/// <summary>
/// Opt-OUT of action logging. Use only when logging the command would be meaningless or recursive
/// (e.g. the command that itself writes a log row) — never to hide a business operation. Every use
/// needs a comment saying why (ADR-011).
/// </summary>
public interface ISkipActionLog;

/// <summary>
/// Optional extra context written with the command's action rows (e.g. "provider=Razorpay;
/// eventId=evt_123"). Identifiers and outcomes only — NEVER request bodies, personal/clinical
/// data, secrets, signatures or tokens (ADR-011).
/// </summary>
public interface IActionLogContext
{
    string? ActionLogContext { get; }
}
