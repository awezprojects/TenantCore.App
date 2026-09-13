namespace TenantCore.Application.Common;

/// <summary>
/// Marks a command whose execution should produce an audit trail entry — one row when it
/// starts, one when it completes or fails. Implemented by opting in on the command record
/// itself (a one-line addition); <see cref="Behaviors.ActionLoggingBehavior{TRequest,TResponse}"/>
/// picks it up automatically. Requests that do not implement this interface are never logged
/// as a business action (queries and internal sub-commands stay silent by default).
/// </summary>
public interface IBusinessAction
{
    /// <summary>Human-readable action name written to the audit trail, e.g. "Patient Registration".</summary>
    string ActionName { get; }
}
