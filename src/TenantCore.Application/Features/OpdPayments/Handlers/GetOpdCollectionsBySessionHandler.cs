using MediatR;
using TenantCore.Application.Features.OpdPayments.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class GetOpdCollectionsBySessionHandler(
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository,
    IOpdRegistrationRepository opdRepository)
    : IRequestHandler<GetOpdCollectionsBySessionQuery, IEnumerable<SessionCollectionDto>>
{
    public async Task<IEnumerable<SessionCollectionDto>> Handle(
        GetOpdCollectionsBySessionQuery request, CancellationToken cancellationToken)
    {
        // DEF-E2E-08/11: payment.CollectedAmount is a running total across every collection event
        // for that registration, however many sessions apart — using it whole here would carry an
        // item collected in a LATER session back into this (earlier, possibly already closed)
        // session's row. Back the item portion out ("base" = fee/full-bill only) and add items by
        // the session they were actually collected in instead.
        var sessionPayments = (await paymentRepository.GetBySessionIdAsync(request.SessionId, request.ApplicationId, cancellationToken)).ToList();
        var sessionParticulars = (await particularRepository.GetCollectedBySessionIdAsync(request.SessionId, request.ApplicationId, cancellationToken)).ToList();

        var itemsCollectedByOpd = sessionParticulars
            .GroupBy(p => p.OpdRegistrationId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var lastItemCollectedAtByOpd = sessionParticulars
            .GroupBy(p => p.OpdRegistrationId)
            .ToDictionary(g => g.Key, g => g.Max(p => p.CollectedAt));

        var registrationIds = sessionPayments.Select(p => p.OpdRegistrationId)
            .Union(itemsCollectedByOpd.Keys)
            .Distinct()
            .ToList();

        if (registrationIds.Count == 0)
            return [];

        // Nominal fee/items totals for each registration, regardless of which session collected them.
        var allPayments = (await paymentRepository.GetByOpdRegistrationIdsAsync(registrationIds, request.ApplicationId, cancellationToken))
            .ToDictionary(p => p.OpdRegistrationId);
        var sessionPaymentByOpd = sessionPayments.ToDictionary(p => p.OpdRegistrationId);
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            registrationIds, request.ApplicationId, cancellationToken);

        var registrations = (await opdRepository.GetByIdsAsync(registrationIds, request.ApplicationId, cancellationToken))
            .ToDictionary(r => r.Id);

        var result = new List<SessionCollectionDto>();
        foreach (var opdId in registrationIds)
        {
            if (!registrations.TryGetValue(opdId, out var registration))
                continue;

            allPayments.TryGetValue(opdId, out var payment);
            var itemsCollectedInThisSession = itemsCollectedByOpd.GetValueOrDefault(opdId, 0m);
            // Home payment's base (fee/full-bill only, items backed out) plus whatever of its items
            // were actually collected in THIS session — 0 for either side when it doesn't apply.
            var baseCollected = sessionPaymentByOpd.TryGetValue(opdId, out var sessionPayment)
                ? sessionPayment.CollectedAmount - itemTotals.GetValueOrDefault(opdId)
                : 0m;
            var collectedThisSession = baseCollected + itemsCollectedInThisSession;

            result.Add(new SessionCollectionDto
            {
                OpdRegistrationId = opdId,
                RegistrationNumber = registration.RegistrationNumber,
                PatientName = registration.Patient is not null
                    ? $"{registration.Patient.FirstName} {registration.Patient.LastName}"
                    : string.Empty,
                DoctorName = registration.DoctorName,
                ConsultationFee = payment?.VisitFee ?? registration.Fee,
                ItemsTotal = payment?.ParticularsTotal ?? 0m,
                TotalCollected = collectedThisSession,
                HasItems = (payment?.ParticularsTotal ?? 0m) > 0,
                CollectedAt = sessionPayment?.AmountReceivedAt ?? lastItemCollectedAtByOpd.GetValueOrDefault(opdId),
                CollectedByUserId = sessionPayment?.ReceivedByUserId
            });
        }

        return result.OrderByDescending(r => r.CollectedAt);
    }
}
