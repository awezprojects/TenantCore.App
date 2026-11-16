using MediatR;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdReports.Handlers;

public sealed class GetIpdRevenueReportHandler(
    IIpdDepositRepository depositRepository,
    IIpdPaymentRepository paymentRepository,
    IApplicationAccessValidator accessValidator)
    : IRequestHandler<GetIpdRevenueReportQuery, IpdRevenueReportDto>
{
    public async Task<IpdRevenueReportDto> Handle(GetIpdRevenueReportQuery request, CancellationToken cancellationToken)
    {
        if (!accessValidator.CanAccess(request.ApplicationId))
            throw new UnauthorizedAccessException("Access denied.");

        var deposits = await depositRepository.GetByDateRangeAsync(request.FromUtc, request.ToUtc, request.ApplicationId, cancellationToken);
        var payments = (await paymentRepository.GetSettledOrRefundedByDateRangeAsync(request.FromUtc, request.ToUtc, request.ApplicationId, cancellationToken)).ToList();

        var totalDeposits = deposits.Sum(d => d.Amount);
        var totalBalanceCollected = payments
            .Where(p => p.SettledAt != null && p.SettledAt >= request.FromUtc && p.SettledAt < request.ToUtc)
            .Sum(p => p.BalanceDue > 0 ? p.BalanceDue : 0);
        var totalRefunded = payments
            .Where(p => p.RefundedAt != null && p.RefundedAt >= request.FromUtc && p.RefundedAt < request.ToUtc)
            .Sum(p => p.RefundDue);

        return new IpdRevenueReportDto(
            totalDeposits, totalBalanceCollected, totalRefunded,
            NetRevenue: totalDeposits + totalBalanceCollected - totalRefunded);
    }
}
