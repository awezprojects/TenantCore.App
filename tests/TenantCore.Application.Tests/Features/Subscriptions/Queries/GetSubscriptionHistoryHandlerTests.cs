using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Queries;

public class GetSubscriptionHistoryHandlerTests
{
    private readonly Mock<IClinicSubscriptionRepository> _repository = new();

    private static SubscriptionPlan CreatePlan() =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999m, "INR", false, false, 1);

    [Fact]
    public async Task Handle_HistoryExists_ReturnsNewestFirst()
    {
        var applicationId = Guid.NewGuid();
        var older = ClinicSubscription.Create(applicationId, CreatePlan(), DateTime.UtcNow.AddDays(-60), "C", "a@b.com", "A");
        var newer = ClinicSubscription.Create(applicationId, CreatePlan(), DateTime.UtcNow.AddDays(-10), "C", "a@b.com", "A");

        // GetHistoryForClinicAsync orders by StartDate descending on the repository side.
        _repository.Setup(r => r.GetHistoryForClinicAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([newer, older]);

        var handler = new GetSubscriptionHistoryHandler(_repository.Object);
        var result = (await handler.Handle(new GetSubscriptionHistoryQuery(applicationId), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmptyList()
    {
        var applicationId = Guid.NewGuid();
        _repository.Setup(r => r.GetHistoryForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = new GetSubscriptionHistoryHandler(_repository.Object);
        var result = await handler.Handle(new GetSubscriptionHistoryQuery(applicationId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
