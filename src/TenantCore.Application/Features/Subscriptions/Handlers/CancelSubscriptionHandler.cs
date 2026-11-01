using MediatR;
using TenantCore.Application.Features.Subscriptions.Commands;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

/// <summary>
/// Retired. This used to set the subscription to Cancelled, which locked the clinic out
/// immediately — despite its own comment promising access until EndDate — and no screen ever
/// called it. The route is kept so an old client gets a clear 409 instead of a 404, but a term
/// can now only be ended by an internal admin, and only before it has started
/// (CancelUpcomingSubscriptionCommand).
/// </summary>
public sealed class CancelSubscriptionHandler : IRequestHandler<CancelSubscriptionCommand>
{
    public Task Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
        => throw new InvalidOperationException(
            "Subscriptions can't be cancelled from the clinic app — contact CloudClinic support.");
}
