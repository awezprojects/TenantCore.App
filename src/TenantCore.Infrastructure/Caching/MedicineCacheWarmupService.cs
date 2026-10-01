using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Caching;

/// <summary>
/// Keeps the system-medicine, medicine-type and dosage-form caches warm for the lifetime of the
/// process. Runs an initial fetch on startup and refreshes every <see cref="RefreshInterval"/>
/// after that — cache population never happens on a live request's clock. A failed refresh
/// leaves the previous snapshot in place and is retried on the next tick; the three caches are
/// only swapped in once every query in a cycle has succeeded, so they never disagree.
/// </summary>
public sealed class MedicineCacheWarmupService(
    IServiceScopeFactory scopeFactory,
    ILogger<MedicineCacheWarmupService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let EF migrations, seeding and other startup queries finish before competing for
        // shared locks on the Medicines table.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAsync(stoppingToken);

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.SetCommandTimeout(QueryTimeout);

                // Cache warmup is a best-effort snapshot — it never participates in any business
                // transaction and refreshes every 30 minutes, so dirty reads are fine. READ
                // UNCOMMITTED means the queries acquire no shared locks and can never be blocked
                // by a concurrent writer holding a lock on the Medicines table.
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED", ct);
            }

            var systemMedicines = await MedicineCacheQueries.LoadSystemMedicinesAsync(dbContext, ct);
            var medicineTypes = await MedicineCacheQueries.LoadMedicineTypesAsync(dbContext, ct);
            var dosageForms = await MedicineCacheQueries.LoadMedicineDosageFormsAsync(dbContext, ct);

            scope.ServiceProvider.GetRequiredService<RefreshableCache<Medicine>>().Set(systemMedicines);
            scope.ServiceProvider.GetRequiredService<RefreshableCache<MedicineType>>().Set(medicineTypes);
            scope.ServiceProvider.GetRequiredService<RefreshableCache<MedicineDosageForm>>().Set(dosageForms);

            logger.LogInformation(
                "Medicine cache refreshed: {SystemMedicines} system medicines, {Types} medicine types, {Forms} dosage forms",
                systemMedicines.Count, medicineTypes.Count, dosageForms.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Medicine cache refresh failed — continuing to serve the previous snapshot");
        }
    }
}
