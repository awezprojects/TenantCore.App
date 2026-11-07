using FluentAssertions;
using Moq;
using TenantCore.Application.Features.FinanceReports.Handlers;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.FinanceReports.Handlers;

public class GetDailyCollectionReportHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IOpdParticularRepository> _particularRepo = new();
    private readonly Mock<IOpdRegistrationRepository> _opdRegistrationRepo = new();

    public GetDailyCollectionReportHandlerTests()
    {
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _particularRepo.Setup(r => r.GetCollectedByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private GetDailyCollectionReportHandler CreateHandler() =>
        new(_paymentRepo.Object, _particularRepo.Object, _opdRegistrationRepo.Object);

    [Fact]
    public async Task Handle_ReturnsPatientNameForEachLineItem()
    {
        // DEF-E2E-04: the daily report hardcoded PatientName = string.Empty on every row.
        var appId = Guid.NewGuid();
        var patient = Patient.Create(appId, "DUMMY", "Patient 012", null, Gender.Male, "9000000000", null, null, null, null);
        var registration = OpdRegistration.Create(appId, patient.Id, Guid.NewGuid(), "Dr. Lee", "OPD-007", 200, null);
        typeof(OpdRegistration).GetProperty("Patient")!.SetValue(registration, patient);

        var payment = OpdPayment.Create(appId, registration.Id, 200);
        payment.AcceptVisitFee(Guid.NewGuid(), Guid.NewGuid());

        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _opdRegistrationRepo.Setup(r => r.GetByIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdRegistration> { registration });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDailyCollectionReportQuery(DateTime.Today, appId), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].PatientName.Should().Be("DUMMY Patient 012");
    }

    [Fact]
    public async Task Handle_WithUtcOffset_QueriesTheLocalDayNotTheUtcDay()
    {
        // DEF-E2E-10: a collection at 02:49 IST on 06-Nov is 21:19 UTC on 05-Nov. Without applying
        // utcOffset to the query bounds, `date=2026-11-06&utcOffset=330` would miss it entirely.
        var appId = Guid.NewGuid();
        const int istOffsetMinutes = 330;
        var requestedLocalDay = new DateTime(2026, 11, 6);

        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _opdRegistrationRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdRegistration>());

        var handler = CreateHandler();
        await handler.Handle(new GetDailyCollectionReportQuery(requestedLocalDay, appId, istOffsetMinutes), CancellationToken.None);

        var expectedUtcStart = requestedLocalDay.AddMinutes(-istOffsetMinutes);
        _paymentRepo.Verify(r => r.GetByDateRangeAsync(expectedUtcStart, It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemCollectedOnADifferentDayThanTheFee_IsCreditedToItsOwnDayNotTheFeeDay()
    {
        // DEF-E2E-12: visit fee received 04-Nov, item collected 06-Nov. The 04-Nov report must not
        // carry the item's money (it should show the fee/full-bill base only); the 06-Nov report
        // must show the item on its own, even though the payment's AmountReceivedAt is 04-Nov.
        var appId = Guid.NewGuid();
        var patient = Patient.Create(appId, "DUMMY", "Patient 027", null, Gender.Male, "9000000001", null, null, null, null);
        var registration = OpdRegistration.Create(appId, patient.Id, Guid.NewGuid(), "Dr. Lee", "OPD-022", 350, null);
        typeof(OpdRegistration).GetProperty("Patient")!.SetValue(registration, patient);

        var payment = OpdPayment.Create(appId, registration.Id, 350);
        payment.UpdateParticularsTotal(400);
        payment.AcceptVisitFee(Guid.NewGuid(), Guid.NewGuid());
        payment.RecordItemCollection(400, Guid.NewGuid(), Guid.NewGuid());

        var item = OpdParticular.Create(appId, registration.Id, Guid.NewGuid(), "Nursing", 400);
        item.Collect(Guid.NewGuid(), Guid.NewGuid());

        // 04-Nov: the fee's own day — payment is in range, no item collected that day.
        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment> { payment });
        _particularRepo.Setup(r => r.GetIndividuallyCollectedTotalsByRegistrationIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(registration.Id)), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, decimal> { [registration.Id] = 400m });
        _opdRegistrationRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdRegistration> { registration });

        var dayOfFee = await CreateHandler().Handle(new GetDailyCollectionReportQuery(new DateTime(2026, 11, 4), appId), CancellationToken.None);
        dayOfFee.GrandTotal.Should().Be(350);

        // 06-Nov: the item's own day — no payment received that day, but the item was collected then.
        _paymentRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdPayment>());
        _particularRepo.Setup(r => r.GetCollectedByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpdParticular> { item });

        var dayOfItem = await CreateHandler().Handle(new GetDailyCollectionReportQuery(new DateTime(2026, 11, 6), appId), CancellationToken.None);
        dayOfItem.GrandTotal.Should().Be(400);
    }
}
