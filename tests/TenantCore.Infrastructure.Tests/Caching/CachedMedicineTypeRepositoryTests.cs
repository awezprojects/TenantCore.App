using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Tests.Caching;

public class CachedMedicineTypeRepositoryTests : IDisposable
{
    private readonly ClinicDbContext _dbContext;
    private readonly RefreshableCache<MedicineType> _cache;
    private readonly CachedMedicineTypeRepository _sut;

    public CachedMedicineTypeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ClinicDbContext(options);

        _cache = new RefreshableCache<MedicineType>();
        var inner = new MedicineTypeRepository(_dbContext);
        _sut = new CachedMedicineTypeRepository(inner, _dbContext, _cache);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetPagedAsync_ColdCache_FallsBackToLiveQuery()
    {
        _dbContext.Set<MedicineType>().Add(MedicineType.Create("Antibiotic", null));
        await _dbContext.SaveChangesAsync();

        _cache.Current.Should().BeNull();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WarmCache_DoesNotReflectDirectDbChangeUntilNextRefresh()
    {
        _dbContext.Set<MedicineType>().Add(MedicineType.Create("Antibiotic", null));
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadMedicineTypesAsync(_dbContext, CancellationToken.None));

        // Inserted directly, bypassing a refresh — must not appear until the next Set().
        _dbContext.Set<MedicineType>().Add(MedicineType.Create("Analgesic", null));
        await _dbContext.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);
        total.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_RefreshesCacheImmediately_SoNextReadReflectsChange()
    {
        _dbContext.Set<MedicineType>().Add(MedicineType.Create("Antibiotic", null));
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadMedicineTypesAsync(_dbContext, CancellationToken.None));

        await _sut.AddAsync(MedicineType.Create("Analgesic", null));
        await _sut.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);

        total.Should().Be(2, "a successful write through the decorator must refresh the cache immediately, with no gap");
    }

    [Fact]
    public async Task GetByNameAsync_AlwaysQueriesLive_NotServedFromCache()
    {
        _dbContext.Set<MedicineType>().Add(MedicineType.Create("Antibiotic", null));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByNameAsync("Antibiotic");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsLiveEntity_MutationsPersistOnSave()
    {
        var type = MedicineType.Create("Antibiotic", null);
        _dbContext.Set<MedicineType>().Add(type);
        await _dbContext.SaveChangesAsync();

        var fetched = await _sut.GetByIdAsync(type.Id);
        fetched.Should().NotBeNull();

        fetched!.Update("Antibiotic Updated", null, isActive: true);
        _sut.Update(fetched);
        await _sut.SaveChangesAsync();

        var persisted = await _dbContext.Set<MedicineType>().AsNoTracking().FirstAsync(t => t.Id == type.Id);
        persisted.Name.Should().Be("Antibiotic Updated");
    }
}
