using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.UsageBilling.Commands;
using TenantCore.Application.Features.UsageBilling.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.UsageBilling.Handlers;

public class ReverseUsageChargeCommandHandlerTests
{
    private readonly Mock<IClinicUsageRecordRepository> _usageRecordRepository = new();
    private readonly Mock<IClinicWalletRepository> _walletRepository = new();
    private readonly Mock<IClinicWalletTransactionRepository> _walletTransactionRepository = new();
    private readonly Mock<ILogger<ReverseUsageChargeCommandHandler>> _logger = new();

    private readonly Guid _appId = Guid.NewGuid();

    private ReverseUsageChargeCommandHandler CreateHandler() => new(
        _usageRecordRepository.Object, _walletRepository.Object, _walletTransactionRepository.Object, _logger.Object);

    private ClinicUsageRecord BuildChargedRecord(decimal amount = 10m)
        => ClinicUsageRecord.Create(_appId, BillingItem.IpdAdmission, Guid.NewGuid(), amount, BillingMode.PayAsYouGo, DateTime.UtcNow);

    [Fact]
    public async Task Handle_RealTopUpHappenedEarlier_ReversalDoesNotOverwriteLastTopUpAt()
    {
        // DEF-IPD-23 (14-Nov): the wallet page's "LAST TOP-UP" must keep showing the real top-up's
        // time, not the time of this reversal credit.
        var record = BuildChargedRecord();
        var wallet = ClinicWallet.Create(_appId, 100m, "INR");
        var realTopUpAt = DateTime.UtcNow.AddDays(-1);
        wallet.Credit(5000m, realTopUpAt);

        _usageRecordRepository.Setup(r => r.GetBySourceAsync(_appId, BillingItem.IpdAdmission, record.SourceEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _walletTransactionRepository.Setup(r => r.ExistsAsync(_appId, WalletReferenceType.UsageRefund, record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _walletRepository.Setup(r => r.GetByApplicationIdAsync(_appId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        var command = new ReverseUsageChargeCommand(_appId, BillingItem.IpdAdmission, record.SourceEntityId, null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Charged.Should().BeTrue();
        wallet.LastTopUpAt.Should().Be(realTopUpAt);
    }

    [Fact]
    public async Task Handle_NoUsageRecord_IsANoOp()
    {
        _usageRecordRepository.Setup(r => r.GetBySourceAsync(_appId, BillingItem.IpdAdmission, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRecord?)null);

        var result = await CreateHandler().Handle(
            new ReverseUsageChargeCommand(_appId, BillingItem.IpdAdmission, Guid.NewGuid(), null, null), CancellationToken.None);

        result.Charged.Should().BeFalse();
        _walletRepository.Verify(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyReversed_IsIdempotentAndDoesNotCreditAgain()
    {
        var record = BuildChargedRecord();
        _usageRecordRepository.Setup(r => r.GetBySourceAsync(_appId, BillingItem.IpdAdmission, record.SourceEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _walletTransactionRepository.Setup(r => r.ExistsAsync(_appId, WalletReferenceType.UsageRefund, record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(
            new ReverseUsageChargeCommand(_appId, BillingItem.IpdAdmission, record.SourceEntityId, null, null), CancellationToken.None);

        result.Charged.Should().BeFalse();
        _walletRepository.Verify(r => r.GetByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
