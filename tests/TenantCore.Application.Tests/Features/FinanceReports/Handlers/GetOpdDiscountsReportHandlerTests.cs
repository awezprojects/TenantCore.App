using FluentAssertions;
using Moq;
using TenantCore.Application.Features.FinanceReports.Handlers;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.FinanceReports.Handlers;

public class GetOpdDiscountsReportHandlerTests
{
    private readonly Mock<IOpdPaymentRepository> _repository = new();

    private GetOpdDiscountsReportHandler CreateHandler() => new(_repository.Object);

    [Fact]
    public async Task Handle_ReturnsDiscountsWithGrandTotal()
    {
        var appId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var discountedAt = new DateTime(2026, 11, 9, 10, 0, 0);

        _repository.Setup(r => r.GetDiscountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string, string, string, decimal, Guid?, DateTime?)>
            {
                (registrationId, "OPD-0001", "Jane Doe", "Dr. Smith", 100m, doctorId, discountedAt)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetOpdDiscountsReportQuery(discountedAt.Date, discountedAt.Date, appId), CancellationToken.None);

        result.GrandTotal.Should().Be(100m);
        result.Discounts.Should().ContainSingle();
        result.Discounts[0].DiscountedByUserId.Should().Be(doctorId);
        result.Discounts[0].PatientName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task Handle_NoDiscountsInRange_ReturnsEmptyWithZeroTotal()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetDiscountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetOpdDiscountsReportQuery(DateTime.Today, DateTime.Today, appId), CancellationToken.None);

        result.Discounts.Should().BeEmpty();
        result.GrandTotal.Should().Be(0m);
    }
}
