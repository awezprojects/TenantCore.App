using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

namespace TenantCore.Infrastructure.Tests.Caching;

public class CachedMedicineDosageFormRepositoryTests : IDisposable
{
    private readonly ClinicDbContext _dbContext;
    private readonly RefreshableCache<MedicineDosageForm> _cache;
    private readonly CachedMedicineDosageFormRepository _sut;

    public CachedMedicineDosageFormRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ClinicDbContext(options);

        _cache = new RefreshableCache<MedicineDosageForm>();
        var inner = new MedicineDosageFormRepository(_dbContext);
        _sut = new CachedMedicineDosageFormRepository(inner, _dbContext, _cache);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetPagedAsync_ColdCache_FallsBackToLiveQuery()
    {
        _dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Tablet", null));
        await _dbContext.SaveChangesAsync();

        _cache.Current.Should().BeNull();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);

        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WarmCache_DoesNotReflectDirectDbChangeUntilNextRefresh()
    {
        _dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Tablet", null));
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadMedicineDosageFormsAsync(_dbContext, CancellationToken.None));

        _dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Syrup", null));
        await _dbContext.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);
        total.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_RefreshesCacheImmediately_SoNextReadReflectsChange()
    {
        _dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Tablet", null));
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadMedicineDosageFormsAsync(_dbContext, CancellationToken.None));

        await _sut.AddAsync(MedicineDosageForm.Create("Syrup", null));
        await _sut.SaveChangesAsync();

        var (_, total) = await _sut.GetPagedAsync(1, 20, null);

        total.Should().Be(2, "a successful write through the decorator must refresh the cache immediately, with no gap");
    }

    [Fact]
    public async Task GetByNameAsync_AlwaysQueriesLive_NotServedFromCache()
    {
        _dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Tablet", null));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByNameAsync("Tablet");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ThroughHandlerPattern_RefreshesCacheAndPersists()
    {
        var form = MedicineDosageForm.Create("Tablet", null);
        _dbContext.Set<MedicineDosageForm>().Add(form);
        await _dbContext.SaveChangesAsync();
        _cache.Set(await MedicineCacheQueries.LoadMedicineDosageFormsAsync(_dbContext, CancellationToken.None));

        var fetched = await _sut.GetByIdAsync(form.Id);
        fetched!.Deactivate();
        _sut.Update(fetched);
        await _sut.SaveChangesAsync();

        var (items, _) = await _sut.GetPagedAsync(1, 20, null);
        items.Should().BeEmpty("the deactivated form is filtered out and the cache must reflect the change immediately");
    }
}
