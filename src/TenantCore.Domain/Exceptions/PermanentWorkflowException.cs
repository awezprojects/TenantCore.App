namespace TenantCore.Domain.Exceptions;

/// <summary>
/// Thrown by an <c>IWorkflowTaskHandler</c> to signal a non-retryable failure — a validation
/// rejection, a code conflict, or any 4xx other than 408/429. The processor stops retrying
/// immediately, marks the task Failed and flags the related request NeedsAttention, instead of
/// backing off and trying again. Any other exception is treated as transient and retried.
/// </summary>
public class PermanentWorkflowException : Exception
{
    public PermanentWorkflowException(string message) : base(message) { }
    public PermanentWorkflowException(string message, Exception innerException) : base(message, innerException) { }
}
