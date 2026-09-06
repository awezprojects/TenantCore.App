using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TenantCore.Application.Services;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;
using System.Net;

namespace TenantCore.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // IErrorLogger is Scoped — it must be resolved per-request via this method parameter,
    // not the constructor (which runs once, from the root provider, at app startup).
    public async Task InvokeAsync(HttpContext context, IErrorLogger errorLogger)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorLogger);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IErrorLogger errorLogger)
    {
        var correlationId = context.TraceIdentifier;

        logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

        var applicationId = context.Items.TryGetValue(ClinicContextMiddleware.ContextKey, out var item) && item is Guid id && id != Guid.Empty
            ? id
            : (Guid?)null;

        await errorLogger.LogExceptionAsync(
            LogCategory.Api,
            "Api.Middleware",
            exception,
            applicationId: applicationId,
            userId: context.User.FindFirst("nameid")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            additionalContext: $"CorrelationId={correlationId}; RequestPath={context.Request.Path}");

        // Detail is the user-facing message shown in the UI.
        // Technical context (exception type, stack trace) stays in the log above.
        var (statusCode, title, detail) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                string.Join(" ", ve.Errors.Select(e => e.ErrorMessage))),

            NotFoundException       => (HttpStatusCode.NotFound,            "Not Found",    UserMessages.NotFound),
            DomainValidationException dve => (HttpStatusCode.BadRequest,    "Invalid Input", dve.Message),
            DomainException de      => (HttpStatusCode.BadRequest,          "Request Error", de.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,    "Unauthorized",  UserMessages.Unauthorized),
            InvalidOperationException ioe => (HttpStatusCode.Conflict,      "Conflict",      ioe.Message),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (
                HttpStatusCode.Conflict, "Conflict", UserMessages.Conflict),
            _ => (HttpStatusCode.InternalServerError, "Server Error",       UserMessages.ServerError)
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["correlationId"] = correlationId;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            problemDetails.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
