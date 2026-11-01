namespace TenantCore.Domain.Exceptions;

/// <summary>
/// Thrown by an <c>IWorkflowTaskHandler</c> when an external precondition has not been met yet
/// but is expected to resolve on its own — for example, a payment link that has been created but
/// not yet paid. Unlike a generic transient failure this is a completely normal, predicted state:
/// the processor retries with the usual backoff but logs at Debug level rather than Warning so
/// the logs are not flooded with alarmist stack-traces for routine waiting states.
/// </summary>
public sealed class WaitingWorkflowException : Exception
{
    public WaitingWorkflowException(string message) : base(message) { }
}
