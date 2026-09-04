using MediatR;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class CancelSubscriptionHandler(IClinicSubscriptionRepository subscriptionRepository)
    : IRequestHandler<CancelSubscriptionCommand>
{
    public async Task Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        // Cross-tenant subscription is treated as not found — never leak its existence.
        if (subscription is null || subscription.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(ClinicSubscription), request.SubscriptionId);

        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("This subscription has already been cancelled.");

        // Cancellation does not refund or shorten the term — EndDate is untouched,
        // the clinic keeps access until it passes. Only a future renewal is prevented.
        subscription.Cancel(request.ActingUserId.ToString());
        subscriptionRepository.Update(subscription);
        await subscriptionRepository.SaveChangesAsync(cancellationToken);
    }
}
