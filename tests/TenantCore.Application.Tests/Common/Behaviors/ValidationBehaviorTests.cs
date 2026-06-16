using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common.Behaviors;

namespace TenantCore.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidatorsConfigured_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            [],
            new Mock<ILogger<ValidationBehavior<TestRequest, string>>>().Object);

        var nextCalled = false;
        var result = await behavior.Handle(
            new TestRequest("valid"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var validator = new TestRequestValidator();
        var behavior = new ValidationBehavior<TestRequest, string>(
            [validator],
            new Mock<ILogger<ValidationBehavior<TestRequest, string>>>().Object);

        var nextCalled = false;
        var result = await behavior.Handle(
            new TestRequest("valid"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionAndSkipsNext()
    {
        var validator = new TestRequestValidator();
        var behavior = new ValidationBehavior<TestRequest, string>(
            [validator],
            new Mock<ILogger<ValidationBehavior<TestRequest, string>>>().Object);

        var nextCalled = false;
        Func<Task> action = () => behavior.Handle(
            new TestRequest(string.Empty),
            () =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    public sealed record TestRequest(string Value);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
