using FluentAssertions;
using Moq;
using TenantCore.Application.Features.FinanceReports.Handlers;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.FinanceReports.Handlers;

public class GetReceptionWiseReportHandlerTests
{
    private readonly Mock<ICounterSessionRepository> _sessionRepo = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();
    private readonly Mock<IExpenseRecordRepository> _expenseRepo = new();

    public GetReceptionWiseReportHandlerTests()
    {
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedBySessionIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private GetReceptionWiseReportHandler CreateHandler() =>
        new(_sessionRepo.Object, _paymentRepo.Object, _particularRepo.Object, _expenseRepo.Object);

    private static CounterSession CreateSessionOpenedAt(Guid applicationId, DateTime openedAt)
    {
        var session = CounterSession.Create(applicationId, Guid.NewGuid(), openedAt.Date);
        typeof(CounterSession).GetProperty("OpenedAt")!.SetValue(session, openedAt);
        return session;
    }

    [Fact]
    public async Task Handle_SingleDayRange_IncludesSessionsOpenedLaterThatSameDay()
    {
        // DEF-E2E-02: `GET ...reception-wise?from=2026-11-04&to=2026-11-04` returned [] because a
        // date-only `To` is midnight, excluding every session opened later that day.
        var appId = Guid.NewGuid();
        var day = new DateTime(2026, 11, 04);
        var session = CreateSessionOpenedAt(appId, day.AddHours(20)); // 20:00 on the requested day

        _sessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CounterSession> { session });
        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseRecord>());

        var handler = CreateHandler();
        var result = (await handler.Handle(new GetReceptionWiseReportQuery(day, day, appId), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].TotalSessions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SessionOpenedNextDay_IsExcluded()
    {
        var appId = Guid.NewGuid();
        var day = new DateTime(2026, 11, 04);
        var session = CreateSessionOpenedAt(appId, day.AddDays(1).AddHours(1)); // next day

        _sessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CounterSession> { session });
        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseRecord>());

        var handler = CreateHandler();
        var result = (await handler.Handle(new GetReceptionWiseReportQuery(day, day, appId), CancellationToken.None)).ToList();

        result.Should().BeEmpty();
    }
}
