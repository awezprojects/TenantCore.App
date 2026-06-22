using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetDailyCollectionReportHandler(IOpdPaymentRepository repository) : IRequestHandler<GetDailyCollectionReportQuery, DailyCollectionReportDto>
{
    public async Task<DailyCollectionReportDto> Handle(GetDailyCollectionReportQuery request, CancellationToken cancellationToken)
    {
        var dayStart = request.Date.Date;
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);

        var payments = await repository.GetByDateRangeAsync(dayStart, dayEnd, request.ApplicationId, cancellationToken);

        var items = payments.Select(p => new DailyCollectionLineItemDto
        {
            PatientName = string.Empty,
            VisitFee = p.VisitFee,
            ParticularsTotal = p.ParticularsTotal,
            Discount = p.Discount,
            FinalAmount = p.FinalAmount
        }).ToList();

        return new DailyCollectionReportDto
        {
            Date = request.Date,
            Items = items,
            GrandTotal = items.Sum(i => i.FinalAmount)
        };
    }
}
