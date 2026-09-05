using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Errors;
using System.Net;

namespace TenantCore.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

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
