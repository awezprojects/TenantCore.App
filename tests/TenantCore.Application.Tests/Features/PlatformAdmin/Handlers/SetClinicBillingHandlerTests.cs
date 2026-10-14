using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Handlers;

/// <summary>
/// The internal admin switching a clinic's billing mode (and, optionally, its per-clinic registration
/// fee and free test days). The rules under test: an immediate switch clears any queue; a queued
/// switch lands on the clinic's coverage end; and a different mode cannot be queued while one is
/// already unresolved — otherwise the guard and the screen would disagree about what is pending.
/// </summary>
public class SetClinicBillingHandlerTests
{
    private readonly Mock<IClinicAccountRepository> _accounts = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptions = new();

    public SetClinicBillingHandlerTests()
    {
        _accounts.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // No coverage by default: a queued change then has nothing to wait for and applies now.
        _subscriptions.Setup(r => r.GetCoverageEndAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);
    }

    private SetClinicBillingHandler CreateHandler() =>
        new(_accounts.Object, _subscriptions.Object, NullLogger<SetClinicBillingHandler>.Instance);

    private ClinicAccount GivenAccount(BillingMode mode = BillingMode.Subscription)
    {
        var account = ClinicAccount.CreateDefault(Guid.NewGuid());
        if (mode != BillingMode.Subscription)
            account.SetBillingMode(mode, DateTime.UtcNow, applyNow: true);

        _accounts.Setup(r => r.GetOrCreateAsync(account.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        return account;
    }

    private void GivenCoverageEnd(Guid applicationId, DateTime? end) =>
        _subscriptions.Setup(r => r.GetCoverageEndAsync(applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(end);

    private static SetClinicBillingCommand Command(
        Guid applicationId, BillingMode mode, bool applyNow = true,
        decimal? fee = null, int? freeTestDays = null)
        => new(applicationId, mode, applyNow, fee, freeTestDays, "Negotiated", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_ApplyNow_AppliesImmediatelyAndSaves()
    {
        var account = GivenAccount(BillingMode.Subscription);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingMode.Should().BeNull();
        account.PendingBillingModeEffectiveFrom.Should().BeNull();
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyOnTheModeWithNothingQueued_WritesNothing()
    {
        var account = GivenAccount(BillingMode.Subscription);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.Subscription), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.Subscription);
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClinicWithNoAccountRow_StillSwitches()
    {
        var account = GivenAccount();

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.PayAsYouGo);
        _accounts.Verify(r => r.GetOrCreateAsync(account.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QueuedChange_LandsOnTheCoverageEnd()
    {
        var account = GivenAccount(BillingMode.Subscription);
        var coverageEnd = DateTime.UtcNow.AddDays(20);
        GivenCoverageEnd(account.ApplicationId, coverageEnd);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.Subscription, "days already paid for are never lost");
        account.PendingBillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingModeEffectiveFrom.Should().Be(coverageEnd);
        account.EffectiveBillingMode(DateTime.UtcNow).Should().Be(BillingMode.Subscription);
    }

    [Fact]
    public async Task Handle_QueuedChangeWithNoCoverage_AppliesNow()
    {
        var account = GivenAccount(BillingMode.Subscription);
        GivenCoverageEnd(account.ApplicationId, null);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingMode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SameModeQueuedAgain_LandsOnTheLaterCoverageEnd()
    {
        var account = GivenAccount(BillingMode.Subscription);
        GivenCoverageEnd(account.ApplicationId, DateTime.UtcNow.AddDays(10));

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);

        var later = DateTime.UtcNow.AddDays(40);
        GivenCoverageEnd(account.ApplicationId, later);
        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);

        account.PendingBillingMode.Should().Be(BillingMode.PayAsYouGo);
        account.PendingBillingModeEffectiveFrom.Should().Be(later);
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DifferentModeQueuedWhileOneIsPending_Throws409BillingModeLocked()
    {
        var account = GivenAccount(BillingMode.Subscription);
        GivenCoverageEnd(account.ApplicationId, DateTime.UtcNow.AddDays(10));

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);

        var action = () => CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.Subscription, applyNow: false), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ErrorCodeException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.ErrorCode.Should().Be(UsageBillingErrorCodes.BillingModeLocked);
        account.PendingBillingMode.Should().Be(BillingMode.PayAsYouGo, "the first queue is left untouched");
    }

    [Fact]
    public async Task Handle_ImmediateSwitchWhileAChangeIsQueued_AppliesAndClearsTheQueue()
    {
        var account = GivenAccount(BillingMode.Subscription);
        GivenCoverageEnd(account.ApplicationId, DateTime.UtcNow.AddDays(10));

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.PayAsYouGo, applyNow: false), CancellationToken.None);
        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.Subscription, applyNow: true), CancellationToken.None);

        account.BillingMode.Should().Be(BillingMode.Subscription);
        account.PendingBillingMode.Should().BeNull("an immediate switch leaves nothing to resolve");
    }

    [Fact]
    public async Task Handle_RegistrationFeeAndFreeTestDays_AreAppliedOnTheSameCall()
    {
        var account = GivenAccount(BillingMode.Subscription);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.Subscription, applyNow: true, fee: 500m, freeTestDays: 45),
            CancellationToken.None);

        account.RegistrationFee.Should().Be(500m);
        account.FreeTestDays.Should().Be(45);
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once,
            "the mode did not change, but the fee and free days did");
    }

    [Fact]
    public async Task Handle_FeeOnlyWhileAlreadyOnTheMode_StillSaves()
    {
        var account = GivenAccount(BillingMode.Subscription);

        await CreateHandler().Handle(
            Command(account.ApplicationId, BillingMode.Subscription, applyNow: true, fee: 250m),
            CancellationToken.None);

        account.RegistrationFee.Should().Be(250m);
        _accounts.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Command_CarriesItsActionNameAndAuditContext()
    {
        var command = Command(Guid.NewGuid(), BillingMode.PayAsYouGo, applyNow: false, freeTestDays: 45);

        command.ActionName.Should().Be("Clinic Billing Mode Set");
        command.ActionLogContext.Should().Contain("mode=PayAsYouGo")
            .And.Contain("queued=true")
            .And.Contain("freeTestDays=45");
    }
}
