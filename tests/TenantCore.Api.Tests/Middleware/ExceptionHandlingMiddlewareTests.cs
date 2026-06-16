using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Api.Middleware;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Errors;

namespace TenantCore.Api.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _logger = new();

    private async Task<JsonDocument> InvokeAndReadResponseAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-correlation-id";

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            _logger.Object);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_Returns400WithValidationErrorTitle()
    {
        var failures = new[] { new ValidationFailure("Name", "Name is required") };
        var exception = new ValidationException(failures);

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_IncludesErrorsInExtensions()
    {
        var failures = new[] { new ValidationFailure("Name", "Name is required") };
        var exception = new ValidationException(failures);

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNotFoundException_Returns404WithNotFoundTitle()
    {
        var exception = new NotFoundException("Patient", Guid.NewGuid());

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
        doc.RootElement.GetProperty("detail").GetString().Should().Be(UserMessages.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WhenDomainValidationException_Returns400WithInvalidInputTitle()
    {
        var exception = new DomainValidationException("Bed is already occupied.");

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Invalid Input");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("Bed is already occupied.");
    }

    [Fact]
    public async Task InvokeAsync_WhenDomainException_Returns400WithRequestErrorTitle()
    {
        var exception = new DomainException("A domain rule was violated.");

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Request Error");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("A domain rule was violated.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns401WithUnauthorizedTitle()
    {
        var exception = new UnauthorizedAccessException();

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Unauthorized");
        doc.RootElement.GetProperty("detail").GetString().Should().Be(UserMessages.Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_Returns409WithConflictTitle()
    {
        var exception = new InvalidOperationException("This record already exists.");

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("This record already exists.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnknownException_Returns500WithServerErrorTitle()
    {
        var exception = new Exception("Something unexpected happened.");

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Server Error");
        doc.RootElement.GetProperty("detail").GetString().Should().Be(UserMessages.ServerError);
    }

    [Fact]
    public async Task InvokeAsync_AllExceptions_IncludeCorrelationIdInResponse()
    {
        var exception = new Exception("any error");

        using var doc = await InvokeAndReadResponseAsync(exception);

        doc.RootElement.TryGetProperty("correlationId", out var correlationIdEl).Should().BeTrue();
        correlationIdEl.GetString().Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task InvokeAsync_AllExceptions_SetsContentTypeToJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new Exception("error"),
            _logger.Object);

        await middleware.InvokeAsync(context);

        // WriteAsJsonAsync normalises the content type; we verify it is JSON.
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndDoesNotWriteErrorResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _logger.Object);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Body.Length.Should().Be(0);
    }
}
