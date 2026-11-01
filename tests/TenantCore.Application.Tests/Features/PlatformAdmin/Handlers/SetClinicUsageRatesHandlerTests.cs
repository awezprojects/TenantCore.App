using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Handlers;

/// <summary>
/// The internal admin setting a clinic's negotiated prices. Deactivate-and-insert preserves the
/// history, an unchanged price writes nothing, and the whole set lands on one SaveChanges so the
/// filtered unique index on (clinic, item) is never momentarily satisfied twice.
/// </summary>
public class SetClinicUsageRatesHandlerTests
{
    private readonly Mock<IClinicUsageRateRepository> _rates = new();
    private readonly List<ClinicUsageRate> _added = [];

    public SetClinicUsageRatesHandlerTests()
    {
        _rates.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _rates.Setup(r => r.AddAsync(It.IsAny<ClinicUsageRate>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicUsageRate, CancellationToken>((rate, _) => _added.Add(rate))
            .Returns(Task.CompletedTask);
    }

    private SetClinicUsageRatesHandler CreateHandler() =>
        new(_rates.Object, NullLogger<SetClinicUsageRatesHandler>.Instance);

    private static ClinicUsageRate ActiveRate(Guid applicationId, BillingItem item, decimal rate) =>
        ClinicUsageRate.Create(applicationId, item, rate, DateTime.UtcNow.AddDays(-5), "admin@example.test");

    private static SetClinicUsageRatesCommand Command(Guid applicationId, params UsageRateSetting[] rates)
        => new(applicationId, rates, "Negotiated", Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_NoExistingRate_InsertsOneAndSaves()
    {
        var applicationId = Guid.NewGuid();
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRate?)null);

        await CreateHandler().Handle(
            Command(applicationId, new UsageRateSetting(BillingItem.OpdRegistration, 15m)),
            CancellationToken.None);

        _added.Should().ContainSingle();
        _added[0].ApplicationId.Should().Be(applicationId);
        _added[0].BillingItem.Should().Be(BillingItem.OpdRegistration);
        _added[0].Rate.Should().Be(15m);
        _added[0].IsActive.Should().BeTrue();
        _added[0].EffectiveFrom.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        _added[0].UpdatedByAdminEmail.Should().Be("admin@example.test");
        _rates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnchangedRate_WritesNothingAtAll()
    {
        var applicationId = Guid.NewGuid();
        var existing = ActiveRate(applicationId, BillingItem.OpdRegistration, 15m);
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(
            Command(applicationId, new UsageRateSetting(BillingItem.OpdRegistration, 15m)),
            CancellationToken.None);

        _added.Should().BeEmpty("the same price is the same row");
        existing.IsActive.Should().BeTrue();
        _rates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ChangedRate_DeactivatesTheOldRowAndInsertsTheReplacement()
    {
        var applicationId = Guid.NewGuid();
        var existing = ActiveRate(applicationId, BillingItem.OpdRegistration, 15m);
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(
            Command(applicationId, new UsageRateSetting(BillingItem.OpdRegistration, 25m)),
            CancellationToken.None);

        existing.IsActive.Should().BeFalse("history is preserved, not overwritten");
        _added.Should().ContainSingle();
        _added[0].Rate.Should().Be(25m);
        _rates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MixedSet_OnlyTheChangedItemIsWrittenAndStillOneSave()
    {
        var applicationId = Guid.NewGuid();
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.OpdRegistration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveRate(applicationId, BillingItem.OpdRegistration, 15m));
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.IpdAdmission, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRate?)null);

        await CreateHandler().Handle(Command(applicationId,
            new UsageRateSetting(BillingItem.OpdRegistration, 15m),
            new UsageRateSetting(BillingItem.IpdAdmission, 40m)), CancellationToken.None);

        _added.Should().ContainSingle();
        _added[0].BillingItem.Should().Be(BillingItem.IpdAdmission);
        _rates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once,
            "the deactivations and their replacements land together");
    }

    [Fact]
    public async Task Handle_ZeroRate_IsAcceptedAsAFreeOperation()
    {
        var applicationId = Guid.NewGuid();
        _rates.Setup(r => r.GetActiveAsync(applicationId, BillingItem.IpdDischarge, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicUsageRate?)null);

        await CreateHandler().Handle(
            Command(applicationId, new UsageRateSetting(BillingItem.IpdDischarge, 0m)), CancellationToken.None);

        _added.Should().ContainSingle();
        _added[0].Rate.Should().Be(0m, "zero means free, which is a legitimate price");
    }

    [Fact]
    public void Command_AuditContextNamesTheItemsAndNeverTheirPrices()
    {
        var command = Command(Guid.NewGuid(),
            new UsageRateSetting(BillingItem.OpdRegistration, 15m),
            new UsageRateSetting(BillingItem.IpdAdmission, 40m));

        command.ActionName.Should().Be("Clinic Usage Rates Set");
        command.ActionLogContext.Should().Contain("OpdRegistration").And.Contain("IpdAdmission");
        command.ActionLogContext.Should().NotContain("15", "the rates themselves live in the handler's audit row");
    }
}
