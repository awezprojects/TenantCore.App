namespace TenantCore.Application.Common.Exceptions;

/// <summary>
/// A non-success response from the Auth API, re-thrown with its original HTTP status
/// and a generic, user-safe message. The raw response body (which can contain internal
/// details such as EF Core exception text) is logged by the caller but never placed on
/// this exception's Message, so ExceptionHandlingMiddleware can't leak it to the client
/// (DEF-UM-02 / DEF-UM-03).
/// </summary>
public sealed class AuthApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
