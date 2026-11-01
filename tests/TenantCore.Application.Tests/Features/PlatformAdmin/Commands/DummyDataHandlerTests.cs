using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Application.Features.PlatformAdmin.Queries;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

/// <summary>
/// The three handlers are deliberately thin — the work lives behind IDummyDataStore. What is worth
/// pinning is that a write returns the post-change status (so the portal never has to re-read to find
/// out what happened), that a store conflict travels out of the handler untouched, and that the
/// action-log context carries identifiers only.
/// </summary>
public class DummyDataHandlerTests
{
    private readonly Mock<IDummyDataStore> _store = new();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();
    private const string AdminEmail = "admin@example.test";

    private static DummyDataStatusDto Status(int patients = 75, int wards = 3) =>
        new() { HasDummyData = patients > 0, PatientCount = patients, WardCount = wards };

    [Fact]
    public async Task GetStatus_AsksTheStoreForThatClinic()
    {
        _store.Setup(s => s.GetStatusAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Status());

        var result = await new GetDummyDataStatusHandler(_store.Object)
            .Handle(new GetDummyDataStatusQuery(_applicationId), CancellationToken.None);

        result.PatientCount.Should().Be(75);
        _store.Verify(s => s.GetStatusAsync(_applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Seed_ReturnsThePostSeedStatus()
    {
        _store.Setup(s => s.SeedAsync(_applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(Status());

        var result = await new SeedDummyDataHandler(_store.Object, Mock.Of<ILogger<SeedDummyDataHandler>>())
            .Handle(new SeedDummyDataCommand(_applicationId, _adminUserId, AdminEmail), CancellationToken.None);

        result.HasDummyData.Should().BeTrue();
        result.WardCount.Should().Be(3);
    }

    [Fact]
    public async Task Seed_AlreadySeeded_LetsTheConflictOutOfTheHandler()
    {
        _store.Setup(s => s.SeedAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("This clinic already has dummy data."));

        var act = async () => await new SeedDummyDataHandler(_store.Object, Mock.Of<ILogger<SeedDummyDataHandler>>())
            .Handle(new SeedDummyDataCommand(_applicationId, _adminUserId, AdminEmail), CancellationToken.None);

        // ExceptionHandlingMiddleware turns InvalidOperationException into a 409.
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Purge_ReturnsTheAllZeroStatus()
    {
        _store.Setup(s => s.PurgeAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Status(patients: 0, wards: 0));

        var result = await new PurgeDummyDataHandler(_store.Object, Mock.Of<ILogger<PurgeDummyDataHandler>>())
            .Handle(new PurgeDummyDataCommand(_applicationId, _adminUserId, AdminEmail), CancellationToken.None);

        result.HasDummyData.Should().BeFalse();
        result.PatientCount.Should().Be(0);
    }

    [Fact]
    public async Task Purge_NothingToPurge_LetsTheConflictOutOfTheHandler()
    {
        _store.Setup(s => s.PurgeAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("This clinic has no dummy data to purge."));

        var act = async () => await new PurgeDummyDataHandler(_store.Object, Mock.Of<ILogger<PurgeDummyDataHandler>>())
            .Handle(new PurgeDummyDataCommand(_applicationId, _adminUserId, AdminEmail), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void BothCommands_AreLoggedBusinessActionsWithIdentifiersOnlyContext()
    {
        var seed = new SeedDummyDataCommand(_applicationId, _adminUserId, AdminEmail);
        var purge = new PurgeDummyDataCommand(_applicationId, _adminUserId, AdminEmail);

        seed.Should().BeAssignableTo<IBusinessAction>();
        purge.Should().BeAssignableTo<IActionLogContext>();

        seed.ActionName.Should().Be("Dummy Data Seeded");
        purge.ActionName.Should().Be("Dummy Data Purged");

        foreach (var context in new[] { seed.ActionLogContext, purge.ActionLogContext })
        {
            context.Should().Contain(_applicationId.ToString()).And.Contain(_adminUserId.ToString());
            context.Should().NotContain(AdminEmail, "the admin's email is personal data (ADR-011)");
        }
    }
}
