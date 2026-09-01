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
        // Money actually collected in this session — payment-level (visit fee, or full bill when discounted)
        // plus any service items collected individually against this session.
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

        var registrations = (await opdRepository.GetByIdsAsync(registrationIds, request.ApplicationId, cancellationToken))
            .ToDictionary(r => r.Id);

        var result = new List<SessionCollectionDto>();
        foreach (var opdId in registrationIds)
        {
            if (!registrations.TryGetValue(opdId, out var registration))
                continue;

            allPayments.TryGetValue(opdId, out var payment);
            var visitFeeCollectedThisSession = sessionPaymentByOpd.TryGetValue(opdId, out var sessionPayment)
                ? sessionPayment.CollectedAmount
                : 0m;
            var itemsCollectedThisSession = itemsCollectedByOpd.GetValueOrDefault(opdId, 0m);

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
                TotalCollected = visitFeeCollectedThisSession + itemsCollectedThisSession,
                HasItems = (payment?.ParticularsTotal ?? 0m) > 0,
                CollectedAt = sessionPayment?.AmountReceivedAt ?? lastItemCollectedAtByOpd.GetValueOrDefault(opdId)
            });
        }

        return result.OrderByDescending(r => r.CollectedAt);
    }
}
