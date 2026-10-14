namespace TenantCore.Application.Common.Exceptions;

/// <summary>
/// A business refusal that carries a stable machine-readable code. The API's
/// ExceptionHandlingMiddleware turns StatusCode into the HTTP status and
/// ErrorCode into problemDetails.Extensions["errorCode"], which the Blazor
/// client keys UI behaviour off (ApiResponseReader).
///
/// This exists because the four usage-billing refusals (an empty wallet, a locked billing mode, an
/// open top-up link) need to be told apart by the client, and the App had no way to emit a code
/// except the two guard middlewares building ProblemDetails by hand. It is deliberately not a
/// DomainException subclass: DomainException maps to 400, and these are 403 and 409.
/// </summary>
public sealed class ErrorCodeException(int statusCode, string errorCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;
}
