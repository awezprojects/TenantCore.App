using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Handlers;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdReports.Handlers;

public class GetIpdRevenueReportHandlerTests
{
    private readonly Mock<IIpdDepositRepository> _depositRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();
    // Wide enough to always contain the real DateTime.UtcNow timestamps CollectBalance/ProcessRefund
    // stamp internally — the test must not depend on when it happens to run.
    private readonly DateTime _from = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _to = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private GetIpdRevenueReportHandler CreateHandler() => new(_depositRepository.Object, _paymentRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_DepositsAndSettlementsAndRefundsInRange_ComputesNetRevenue()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);

        var deposit1 = IpdDeposit.Create(_appId, Guid.NewGuid(), 2000m, Guid.NewGuid(), null, null);
        var deposit2 = IpdDeposit.Create(_appId, Guid.NewGuid(), 3000m, Guid.NewGuid(), null, null);
        _depositRepository.Setup(r => r.GetByDateRangeAsync(_from, _to, _appId, It.IsAny<CancellationToken>())).ReturnsAsync([deposit1, deposit2]);

        var settledPayment = IpdPayment.Create(_appId, Guid.NewGuid(), 1000m);
        settledPayment.FinalizeAtDischarge(0, _from.AddDays(1)); // BalanceDue = 1000, still Pending
        settledPayment.CollectBalance(Guid.NewGuid(), null); // SettledAt = now (in range, since "now" is after _from)

        var refundedPayment = IpdPayment.Create(_appId, Guid.NewGuid(), 1000m);
        refundedPayment.IncreaseDeposited(5000m);
        refundedPayment.FinalizeAtDischarge(0, _from.AddDays(2)); // RefundDue = 4000, PendingRefund
        refundedPayment.ProcessRefund(Guid.NewGuid()); // RefundedAt = now

        _paymentRepository.Setup(r => r.GetSettledOrRefundedByDateRangeAsync(_from, _to, _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([settledPayment, refundedPayment]);

        var result = await CreateHandler().Handle(new GetIpdRevenueReportQuery(_appId, _from, _to), CancellationToken.None);

        result.TotalDeposits.Should().Be(5000m);
        result.TotalBalanceCollected.Should().Be(1000m);
        result.TotalRefunded.Should().Be(4000m);
        result.NetRevenue.Should().Be(5000m + 1000m - 4000m);
    }
}
