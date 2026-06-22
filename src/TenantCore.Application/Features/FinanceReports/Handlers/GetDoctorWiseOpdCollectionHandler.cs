using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetDoctorWiseOpdCollectionHandler(IOpdPaymentRepository repository)
    : IRequestHandler<GetDoctorWiseOpdCollectionQuery, DoctorWiseCollectionDto>
{
    public async Task<DoctorWiseCollectionDto> Handle(GetDoctorWiseOpdCollectionQuery request, CancellationToken cancellationToken)
    {
        var offset  = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);

        var totals = (await repository.GetDoctorWiseTotalsAsync(
            fromUtc, toUtc, request.ApplicationId, request.DoctorUserId, cancellationToken))
            .Select(t => new DoctorWiseTotalDto
            {
                DoctorUserId = t.DoctorUserId,
                DoctorName   = t.DoctorName,
                TotalAmount  = t.Total
            })
            .ToList();

        return new DoctorWiseCollectionDto
        {
            Doctors    = totals,
            GrandTotal = totals.Sum(t => t.TotalAmount)
        };
    }
}
