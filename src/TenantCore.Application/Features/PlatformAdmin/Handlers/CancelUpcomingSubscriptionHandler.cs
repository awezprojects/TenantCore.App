using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Cancels a term the clinic has paid for (or been granted) but has not started using, then pulls
/// every later term earlier so the chain stays contiguous — otherwise cancelling a queued term
/// would leave the clinic locked out for exactly that term's length.
///
/// A term that has already started is never cancelled here: the clinic is using it.
/// Money already taken is NOT refunded automatically — that is a manual Razorpay action.
/// </summary>
public sealed class CancelUpcomingSubscriptionHandler(
    IClinicSubscriptionRepository subscriptionRepository,
    ILogger<CancelUpcomingSubscriptionHandler> logger)
    : IRequestHandler<CancelUpcomingSubscriptionCommand>
{
    public async Task Handle(CancelUpcomingSubscriptionCommand command, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(command.SubscriptionId, ct);

        // A subscription belonging to another clinic is treated as not found — never leak it.
        if (subscription is null || subscription.ApplicationId != command.ApplicationId)
            throw new NotFoundException(nameof(ClinicSubscription), command.SubscriptionId);

        var utcNow = DateTime.UtcNow;

        // Throws InvalidOperationException (→ 409) when the term has already started.
        subscription.CancelUpcoming(command.AdminEmail, command.Reason, utcNow);

        var rescheduled = await RechainLaterTermsAsync(command.ApplicationId, subscription.Id, utcNow, ct);

        await subscriptionRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin cancelled upcoming subscription {SubscriptionId} for clinic {ApplicationId}; {Rescheduled} later term(s) moved earlier.",
            subscription.Id, command.ApplicationId, rescheduled);
    }

    /// <summary>
    /// Re-dates the remaining upcoming terms in order, each starting where the previous coverage
    /// ends. Durations are preserved, so every term still runs its full length.
    /// </summary>
    private async Task<int> RechainLaterTermsAsync(Guid applicationId, Guid cancelledId, DateTime utcNow, CancellationToken ct)
    {
        var upcoming = await subscriptionRepository.GetUpcomingForClinicAsync(applicationId, utcNow, ct);
        var remaining = upcoming.Where(s => s.Id != cancelledId).OrderBy(s => s.StartDate).ToList();
        if (remaining.Count == 0)
            return 0;

        // The current term's end, if one is running; otherwise the chain restarts from now.
        var current = await subscriptionRepository.GetActiveForClinicAsync(applicationId, ct);
        var nextStart = current?.EndDate ?? utcNow;

        var rescheduled = 0;
        foreach (var term in remaining)
        {
            if (term.StartDate != nextStart)
            {
                term.Reschedule(nextStart, utcNow);
                rescheduled++;
            }

            nextStart = term.EndDate;
        }

        return rescheduled;
    }
}
