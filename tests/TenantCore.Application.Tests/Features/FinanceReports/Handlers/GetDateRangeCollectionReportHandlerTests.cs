using FluentAssertions;
using Moq;
using TenantCore.Application.Features.FinanceReports.Handlers;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.FinanceReports.Handlers;

public class GetDateRangeCollectionReportHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();

    private GetDateRangeCollectionReportHandler CreateHandler() => new(_paymentRepo.Object, _particularRepo.Object);

    [Fact]
    public async Task Handle_WithUtcOffset_BucketsByLocalDayNotUtcDay()
    {
        // DEF-E2E-10: a ₹100 collection at 02:49 IST on 06-Nov is stored as 05-Nov 21:19 UTC.
        // The query bounds were already offset-aware, but the per-day grouping key used the raw
        // UTC date, so this landed on 05-Nov instead of 06-Nov.
        var appId = Guid.NewGuid();
        const int istOffsetMinutes = 330;
        var collectedAtUtc = new DateTime(2026, 11, 5, 21, 19, 0, DateTimeKind.Utc);

        var payment = OpdPayment.Create(appId, Guid.NewGuid(), 100);
        payment.AcceptVisitFee(Guid.NewGuid(), Guid.NewGuid());
        typeof(OpdPayment).GetProperty("AmountReceivedAt")!.SetValue(payment, collectedAtUtc);

        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetDateRangeCollectionReportQuery(new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), appId, istOffsetMinutes),
            CancellationToken.None);

        result.DayTotals.Should().ContainSingle(d => d.Date == new DateTime(2026, 11, 6) && d.Amount == 100);
        result.GrandTotal.Should().Be(100);
    }
}
