using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetDailyCollectionReportHandler(
    IOpdPaymentRepository repository,
    IOpdParticularRepository particularRepository,
    IOpdRegistrationRepository opdRegistrationRepository) : IRequestHandler<GetDailyCollectionReportQuery, DailyCollectionReportDto>
{
    public async Task<DailyCollectionReportDto> Handle(GetDailyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        // DEF-E2E-10: AmountReceivedAt is stored in UTC; shift the day boundary by the caller's
        // utcOffset so a local-day request lines up with local-day collections, not UTC ones.
        var offset = request.UtcOffsetMinutes;
        var dayStart = request.Date.Date.AddMinutes(-offset);
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);

        var paymentsList = (await repository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken)).ToList();

        // DEF-E2E-12: a payment's CollectedAmount is a running total across every collection event
        // for that registration, however many days apart — crediting it whole to the day the visit
        // fee was first received would also carry in items collected on a later day. Back the item
        // portion out ("base" = fee/full-bill only) and list items actually collected today on
        // their own, via their own CollectedAt, regardless of which day their payment was received.
        var itemTotals = await particularRepository.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
            paymentsList.Select(p => p.OpdRegistrationId), request.ApplicationId, cancellationToken);
        var itemsCollectedToday = (await particularRepository.GetCollectedByDateRangeAsync(
            dayStart, dayStart.AddDays(1), request.ApplicationId, cancellationToken)).ToList();

        var registrationIds = paymentsList.Select(p => p.OpdRegistrationId)
            .Union(itemsCollectedToday.Select(i => i.OpdRegistrationId))
            .Distinct();
        var registrations = await opdRegistrationRepository.GetByIdsAsync(registrationIds, request.ApplicationId, cancellationToken);
        var patientNamesById = registrations.ToDictionary(r => r.Id, r => $"{r.Patient.FirstName} {r.Patient.LastName}".Trim());

        var items = paymentsList.Select(p => new DailyCollectionLineItemDto
        {
            PatientName = patientNamesById.GetValueOrDefault(p.OpdRegistrationId, string.Empty),
            VisitFee = p.VisitFee,
            ParticularsTotal = p.ParticularsTotal,
            Discount = p.Discount,
            FinalAmount = p.CollectedAmount - itemTotals.GetValueOrDefault(p.OpdRegistrationId)
        }).ToList();

        // Items collected today for a visit whose fee was received on a different day get their
        // own row — their money belongs to today, not to the fee's receipt day.
        items.AddRange(itemsCollectedToday.Select(i => new DailyCollectionLineItemDto
        {
            PatientName = patientNamesById.GetValueOrDefault(i.OpdRegistrationId, string.Empty),
            VisitFee = 0,
            ParticularsTotal = i.Amount,
            Discount = 0,
            FinalAmount = i.Amount
        }));

        return new DailyCollectionReportDto
        {
            Date = request.Date,
            Items = items,
            GrandTotal = items.Sum(i => i.FinalAmount)
        };
    }
}
