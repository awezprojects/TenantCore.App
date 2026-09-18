using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Obstetrics.Helpers;

/// <summary>
/// The tenure table is what the EDD Overdue list reads, so an LMP that arrives with a saved
/// prescription must create/refresh the patient's active pregnancy tenure — previously only the
/// dedicated SetObstetricLmp endpoint did that, leaving the overdue list permanently empty.
/// </summary>
public class PregnancyTenureSyncTests
{
    private readonly Mock<IPregnancyTenureRepository> _repository = new();

    [Fact]
    public async Task SyncFromLmpAsync_WhenNoActiveTenure_CreatesActiveTenureWithEddByLmp()
    {
        var appId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-56);
        PregnancyTenure? added = null;

        _repository.Setup(r => r.GetActiveForPatientAsync(patientId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PregnancyTenure?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<PregnancyTenure>(), It.IsAny<CancellationToken>()))
            .Callback<PregnancyTenure, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);

        await PregnancyTenureSync.SyncFromLmpAsync(_repository.Object, patientId, appId, lmp);

        added.Should().NotBeNull();
        added!.PatientId.Should().Be(patientId);
        added.ApplicationId.Should().Be(appId);
        added.Lmp.Should().Be(lmp);
        added.EddByLmp.Should().Be(lmp.AddDays(280));
        added.Status.Should().Be(PregnancyTenureStatus.Active);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncFromLmpAsync_WhenActiveTenureNotOverdue_RefreshesLmpAndEdd()
    {
        var appId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var lmp = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-20);
        var tenure = new PregnancyTenure
        {
            PatientId = patientId,
            ApplicationId = appId,
            Lmp = lmp.AddDays(-5),
            EddByLmp = lmp.AddDays(-5).AddDays(280),
            Status = PregnancyTenureStatus.Active
        };

        _repository.Setup(r => r.GetActiveForPatientAsync(patientId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenure);

        await PregnancyTenureSync.SyncFromLmpAsync(_repository.Object, patientId, appId, lmp);

        tenure.Lmp.Should().Be(lmp);
        tenure.EddByLmp.Should().Be(lmp.AddDays(280));
        _repository.Verify(r => r.Update(tenure), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncFromLmpAsync_WhenActiveTenureIsOverdue_LeavesItUntouched()
    {
        var appId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var overdueEdd = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var tenure = new PregnancyTenure
        {
            PatientId = patientId,
            ApplicationId = appId,
            Lmp = overdueEdd.AddDays(-280),
            EddByLmp = overdueEdd,
            Status = PregnancyTenureStatus.Active
        };

        _repository.Setup(r => r.GetActiveForPatientAsync(patientId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenure);

        await PregnancyTenureSync.SyncFromLmpAsync(
            _repository.Object, patientId, appId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30));

        tenure.EddByLmp.Should().Be(overdueEdd);
        _repository.Verify(r => r.Update(It.IsAny<PregnancyTenure>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncFromLmpAsync_WithoutLmp_DoesNothing()
    {
        await PregnancyTenureSync.SyncFromLmpAsync(
            _repository.Object, Guid.NewGuid(), Guid.NewGuid(), null);

        _repository.Verify(
            r => r.GetActiveForPatientAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}