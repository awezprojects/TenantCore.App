namespace TenantCore.Domain.Exceptions;

/// <summary>Maps to HTTP 429 — a caller-side rate limit (e.g. the "check payment" 30-second throttle), distinct from a 409 state conflict.</summary>
public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message) { }
}
