using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Tests.Caching;

public class MedicineCacheWarmupServiceTests
{
    [Fact]
    public async Task StartAsync_PopulatesAllThreeCaches_FromDatabase()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<ClinicDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<RefreshableCache<Medicine>>();
        services.AddSingleton<RefreshableCache<MedicineType>>();
        services.AddSingleton<RefreshableCache<MedicineDosageForm>>();
        var provider = services.BuildServiceProvider();

        using (var seedScope = provider.CreateScope())
        {
            var dbContext = seedScope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            dbContext.Set<Medicine>().Add(Medicine.Create(
                "Paracetamol", null, null, null, null, null, null, null, null,
                isGeneric: false, null, null, null, null, null, medicineTypeId: null, applicationId: null));
            dbContext.Set<MedicineType>().Add(MedicineType.Create("Antibiotic", null));
            dbContext.Set<MedicineDosageForm>().Add(MedicineDosageForm.Create("Tablet", null));
            await dbContext.SaveChangesAsync();
        }

        var service = new MedicineCacheWarmupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MedicineCacheWarmupService>.Instance);

        var medicineCache = provider.GetRequiredService<RefreshableCache<Medicine>>();
        var typeCache = provider.GetRequiredService<RefreshableCache<MedicineType>>();
        var formCache = provider.GetRequiredService<RefreshableCache<MedicineDosageForm>>();

        await service.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => medicineCache.Current is not null, TimeSpan.FromSeconds(5));

            medicineCache.Current.Should().ContainSingle(m => m.Name == "Paracetamol");
            typeCache.Current.Should().ContainSingle(t => t.Name == "Antibiotic");
            formCache.Current.Should().ContainSingle(f => f.Name == "Tablet");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(20);
        }
    }
}
