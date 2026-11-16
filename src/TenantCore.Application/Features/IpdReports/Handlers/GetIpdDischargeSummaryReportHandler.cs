using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.IpdReports.Handlers;

// Patient name/phone are shown on screen and in print only — never written to the Azure
// Table log rows (no-PII rule, ADR-011). This query itself is not an IBusinessAction/Command,
// so nothing about it is action-logged beyond the standard ApiRequestLogs failure/slow path.
public sealed class GetIpdDischargeSummaryReportHandler(
    IIpdRegistrationRepository ipdRepository,
    IIpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdDischargeSummaryReportQuery, IpdDischargeSummaryReportDto>
{
    public async Task<IpdDischargeSummaryReportDto> Handle(GetIpdDischargeSummaryReportQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var discharges = (await ipdRepository.GetDischargesByDateRangeAsync(request.FromUtc, request.ToUtc, request.ApplicationId, cancellationToken)).ToList();
        var payments = (await paymentRepository.GetByRegistrationIdsAsync(discharges.Select(d => d.Id), request.ApplicationId, cancellationToken))
            .ToDictionary(p => p.IpdRegistrationId);

        var rows = discharges.Select(d =>
        {
            payments.TryGetValue(d.Id, out var payment);

            // Nothing outstanding: the balance (if any) has been collected (or none was ever
            // due) and no refund is still pending. BalanceDue itself is a locked snapshot from
            // finalization, not a running balance — CollectBalance/ProcessRefund never zero it.
            var settled = payment is not null &&
                          payment.PaymentStatus == PaymentStatus.Received &&
                          payment.RefundStatus != RefundStatus.PendingRefund;

            // OBS-7: once settled, show 0 here instead of the raw locked snapshot — a refunded
            // admission's BalanceDue stays negative forever by design, which otherwise reads as
            // "still owed" on this report even though nothing is actually outstanding. The raw
            // number is still exactly what prints on the Final Bill, where the "Balance"/"Refund
            // due" label gives it context.
            var displayBalance = payment is null ? (decimal?)null : settled ? 0m : payment.BalanceDue;

            return new IpdDischargeSummaryRowDto(
                d.Id,
                d.AdmissionNumber,
                $"{d.Patient.FirstName} {d.Patient.LastName}",
                d.Patient.PhoneNumber,
                d.AdmissionDate,
                d.DischargeDate!.Value,
                payment?.FinalAmount,
                displayBalance,
                Settled: settled,
                HasBill: payment is not null);
        }).ToList();

        return new IpdDischargeSummaryReportDto(rows);
    }
}
