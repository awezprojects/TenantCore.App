using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TenantCore.Domain.Exceptions;
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

        var (statusCode, title, detail) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),
            NotFoundException ne => (HttpStatusCode.NotFound, "Not Found", ne.Message),
            DomainValidationException dve => (HttpStatusCode.BadRequest, "Domain Validation Error", dve.Message),
            DomainException de => (HttpStatusCode.BadRequest, "Domain Error", de.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "You are not authorized."),
            InvalidOperationException ioe => (HttpStatusCode.Conflict, "Conflict", ioe.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
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
