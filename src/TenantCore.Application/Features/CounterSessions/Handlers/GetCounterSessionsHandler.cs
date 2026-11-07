using MediatR;
using TenantCore.Application.Features.CounterSessions.Queries;
using TenantCore.Application.Features.CounterSessions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.CounterSessions.Handlers;

public sealed class GetCounterSessionsHandler(
    ICounterSessionRepository repository,
    IOpdPaymentRepository paymentRepository,
    IOpdParticularRepository particularRepository)
    : IRequestHandler<GetCounterSessionsQuery, IEnumerable<CounterSessionSummaryDto>>
{
    public async Task<IEnumerable<CounterSessionSummaryDto>> Handle(GetCounterSessionsQuery request, CancellationToken cancellationToken)
    {
        // Use ToLocalTime() to normalise both UTC-stored and Unspecified-stored SessionDate values
        // so the date comparison is always against the local calendar date.
        var sessions = (await repository.GetAllAsync(cancellationToken))
            .Where(s => s.ApplicationId == request.ApplicationId
                && (request.From == null || s.SessionDate.ToLocalTime().Date >= request.From.Value.Date)
                && (request.To == null || s.SessionDate.ToLocalTime().Date <= request.To.Value.Date))
            .OrderByDescending(s => s.SessionDate)
            .ToList();

        // DEF-E2E-08/11: back each payment down to its fee/full-bill "base" (CollectedAmount minus
        // every individually collected item for that registration, wherever collected), then add
        // items by the session they were actually collected in instead of the payment's own.
        var liveBySessionId = new Dictionary<Guid, decimal>();
        foreach (var open in sessions.Where(s => s.Status == CounterSessionStatus.Open))
        {
            var payments = await paymentRepository.GetBySessionIdAsync(open.Id, open.ApplicationId, cancellationToken);
            var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
                payments.Select(p => p.OpdRegistrationId), open.ApplicationId, cancellationToken);
            var baseCollected = payments.Sum(p => p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId));
            var itemsCollectedHere = (await particularRepository.GetCollectedBySessionIdAsync(open.Id, open.ApplicationId, cancellationToken)).Sum(i => i.Amount);
            liveBySessionId[open.Id] = baseCollected + itemsCollectedHere;
        }

        return sessions.Select(s =>
        {
            var dto = CounterSessionTranslator.ToSummaryDto(s);
            if (!liveBySessionId.TryGetValue(s.Id, out var live)) return dto;
            return new CounterSessionSummaryDto
            {
                Id = dto.Id,
                SessionDate = dto.SessionDate,
                OpenedAt = dto.OpenedAt,
                Status = dto.Status,
                OpenedByUserId = dto.OpenedByUserId,
                TotalCollected = live,
                TotalExpenses = dto.TotalExpenses,
                NetAmount = live - dto.TotalExpenses
            };
        });
    }
}
