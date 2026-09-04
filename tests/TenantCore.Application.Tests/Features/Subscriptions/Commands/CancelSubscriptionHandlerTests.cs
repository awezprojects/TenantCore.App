using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

public class CancelSubscriptionHandlerTests
{
    private readonly Mock<IClinicSubscriptionRepository> _repository = new();

    private static SubscriptionPlan CreatePlan() =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d", 30, 999m, "INR", false, false, 1);

    [Fact]
    public async Task Handle_ActiveSubscription_SetsCancelledStatusAndLeavesEndDateUntouched()
    {
        var applicationId = Guid.NewGuid();
        var actingUserId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(), DateTime.UtcNow, "Clinic", "a@b.com", "Admin");
        var originalEndDate = subscription.EndDate;

        _repository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CancelSubscriptionHandler(_repository.Object);
        await handler.Handle(new CancelSubscriptionCommand(applicationId, subscription.Id, actingUserId), CancellationToken.None);

        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancelledBy.Should().Be(actingUserId.ToString());
        subscription.EndDate.Should().Be(originalEndDate);

        _repository.Verify(r => r.Update(subscription), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ClinicSubscription?)null);

        var handler = new CancelSubscriptionHandler(_repository.Object);
        var action = () => handler.Handle(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SubscriptionBelongsToDifferentTenant_ThrowsNotFoundException()
    {
        var subscription = ClinicSubscription.Create(Guid.NewGuid(), CreatePlan(), DateTime.UtcNow, "Clinic", "a@b.com", "Admin");
        _repository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var handler = new CancelSubscriptionHandler(_repository.Object);
        var action = () => handler.Handle(new CancelSubscriptionCommand(Guid.NewGuid(), subscription.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var subscription = ClinicSubscription.Create(applicationId, CreatePlan(), DateTime.UtcNow, "Clinic", "a@b.com", "Admin");
        subscription.Cancel("someone");

        _repository.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var handler = new CancelSubscriptionHandler(_repository.Object);
        var action = () => handler.Handle(new CancelSubscriptionCommand(applicationId, subscription.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
