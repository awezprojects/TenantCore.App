using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common.Behaviors;
using TenantCore.Application.Tests.Common.Logging;

namespace TenantCore.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestProcessed_LogsStartAndEndAndReturnsResponse()
    {
        var request = new TestRequest("request");
        const string expectedResponse = "response";
        var logger = new Mock<ILogger<LoggingBehavior<TestRequest, string>>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger.Object);
        var nextCalled = false;

        var result = await behavior.Handle(
            request,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(expectedResponse);
            },
            CancellationToken.None);

        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();
        logger.VerifyLog(LogLevel.Information, "Handling", Times.Once());
        logger.VerifyLog(LogLevel.Information, "Handled", Times.Once());
    }

    public sealed record TestRequest(string Value);
}
