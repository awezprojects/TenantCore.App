using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetOpdDiscountsReportHandler(IOpdPaymentRepository repository)
    : IRequestHandler<GetOpdDiscountsReportQuery, OpdDiscountReportDto>
{
    public async Task<OpdDiscountReportDto> Handle(GetOpdDiscountsReportQuery request, CancellationToken cancellationToken)
    {
        var offset  = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);

        var entries = (await repository.GetDiscountsAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken))
            .Select(d => new OpdDiscountEntryDto
            {
                OpdRegistrationId = d.OpdRegistrationId,
                RegistrationNumber = d.RegistrationNumber,
                PatientName = d.PatientName,
                DoctorName = d.DoctorName,
                DiscountAmount = d.DiscountAmount,
                DiscountedByUserId = d.DiscountedByUserId,
                DiscountedAt = d.DiscountedAt
            })
            .OrderByDescending(d => d.DiscountedAt)
            .ToList();

        return new OpdDiscountReportDto
        {
            Discounts = entries,
            GrandTotal = entries.Sum(d => d.DiscountAmount)
        };
    }
}
