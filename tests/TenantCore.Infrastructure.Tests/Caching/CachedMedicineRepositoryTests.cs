using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Tests.Caching;

public class CachedMedicineRepositoryTests : IDisposable
{
    private readonly ClinicDbContext _dbContext;
    private readonly RefreshableCache<Medicine> _cache;
    private readonly CachedMedicineRepository _sut;

    public CachedMedicineRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ClinicDbContext(options);

        _cache = new RefreshableCache<Medicine>();
        var inner = new MedicineRepository(_dbContext);
        _sut = new CachedMedicineRepository(inner, _dbContext, _cache);
    }

    public void Dispose() => _dbContext.Dispose();

    private static Medicine CreateMedicine(string name, Guid? applicationId) =>
        Medicine.Create(
            name, null, null, null, null, null, null, null, null,
            isGeneric: false, null, null, null, null, null, medicineTypeId: null,
            applicationId: applicationId);

    [Fact]
    public async Task GetPagedAsync_ColdCache_FallsBackToLiveQuery_NeverBlocksOrThrows()
    {
        // Simulates a request arriving before MedicineCacheWarmupService's first refresh has
        // completed — RefreshableCache<Medicine>.Current is still null.
        var clinicId = Guid.NewGuid();
        _dbContext.Set<Medicine>().Add(CreateMedicine("Paracetamol", applicationId: null));
        _dbContext.Set<Medicine>().Add(CreateMedicine("ClinicOwnMed", applicationId: clinicId));
        await _dbContext.SaveChangesAsync();

        _cache.Current.Should().BeNull();

        var (items, total) = await _sut.GetPagedAsync(
            1, 20, search: null, brandName: null, genericName: null,
            medicineTypeId: null, dosageFormId: null, isGeneric: null, clinicId);

        total.Should().Be(2);
        items.Select(m => m.Name).Should().BeEquivalentTo(["Paracetamol", "ClinicOwnMed"]);
    }

    [Fact]
    public async Task GetPagedAsync_NeverReturnsAnotherClinicsOwnMedicine()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        _dbContext.Set<Medicine>().Add(CreateMedicine("ClinicAOnlyMed", applicationId: clinicA));
        await _dbContext.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(
            1, 20, search: null, brandName: null, genericName: null,
            medicineTypeId: null, dosageFormId: null, isGeneric: null, clinicB);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedAsync_WarmCache_DoesNotReflectDirectDbChangeUntilNextRefresh()
    {
        var clinicId = Guid.NewGuid();
        _dbContext.Set<Medicine>().Add(CreateMedicine("Paracetamol", applicationId: null));
        await _dbContext.SaveChangesAsync();

        // Simulate MedicineCacheWarmupService having already completed a refresh.
        var warmSnapshot = await MedicineCacheQueries.LoadSystemMedicinesAsync(_dbContext, CancellationToken.None);
        _cache.Set(warmSnapshot);

        // A system-wide medicine inserted directly (bypassing a refresh) must not show up —
        // the cache is only ever replaced by an explicit Set(), never re-queried on read.
        _dbContext.Set<Medicine>().Add(CreateMedicine("Ibuprofen", applicationId: null));
        await _dbContext.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null, null, null, null, null, null, clinicId);

        total.Should().Be(1, "the warm snapshot is served as-is until the next background refresh calls Set() again");
    }

    [Fact]
    public async Task GetPagedAsync_ReflectsNewClinicMedicineImmediately_EvenWithWarmCache()
    {
        var clinicId = Guid.NewGuid();
        _dbContext.Set<Medicine>().Add(CreateMedicine("Paracetamol", applicationId: null));
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadSystemMedicinesAsync(_dbContext, CancellationToken.None));

        _dbContext.Set<Medicine>().Add(CreateMedicine("NewClinicMed", applicationId: clinicId));
        await _dbContext.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null, null, null, null, null, null, clinicId);

        total.Should().Be(2, "a clinic's own medicines are always queried live, never cached");
    }

    [Fact]
    public async Task GetByIdWithTypeAsync_SystemMedicine_ServedFromWarmCache()
    {
        var med = CreateMedicine("Paracetamol", applicationId: null);
        _dbContext.Set<Medicine>().Add(med);
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadSystemMedicinesAsync(_dbContext, CancellationToken.None));

        var result = await _sut.GetByIdWithTypeAsync(med.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(med.Id);
    }

    [Fact]
    public async Task FindSimilarAsync_AlwaysQueriesLive_NotServedFromCache()
    {
        var clinicId = Guid.NewGuid();
        var med = CreateMedicine("Paracetamol", applicationId: clinicId);
        _dbContext.Set<Medicine>().Add(med);
        await _dbContext.SaveChangesAsync();

        var similar = await _sut.FindSimilarAsync("Paracetamol", null, null, clinicId);

        similar.Should().ContainSingle(m => m.Id == med.Id);
    }

    [Fact]
    public async Task GetByNamePrefixAsync_MergesSystemAndClinicMedicines()
    {
        var clinicId = Guid.NewGuid();
        _dbContext.Set<Medicine>().Add(CreateMedicine("Paracetamol500", applicationId: null));
        _dbContext.Set<Medicine>().Add(CreateMedicine("Paracetamol650", applicationId: clinicId));
        await _dbContext.SaveChangesAsync();

        var results = await _sut.GetByNamePrefixAsync("Paracetamol", clinicId, limit: 10);

        results.Select(m => m.Name).Should().BeEquivalentTo(["Paracetamol500", "Paracetamol650"]);
    }
}
