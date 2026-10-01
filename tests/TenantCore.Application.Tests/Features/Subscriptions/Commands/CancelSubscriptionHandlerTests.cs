using FluentAssertions;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

/// <summary>
/// The endpoint is retired. It used to mark the subscription Cancelled, which locked the clinic out
/// immediately even though its own comment promised access until EndDate. A term can now only be
/// ended by an internal admin, and only before it has started.
/// </summary>
public class CancelSubscriptionHandlerTests
{
    [Fact]
    public async Task Handle_AnyRequest_ThrowsInvalidOperationExceptionDirectingToSupport()
    {
        var handler = new CancelSubscriptionHandler();

        var action = () => handler.Handle(
            new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        (await action.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*contact CloudClinic support*");
    }
}
