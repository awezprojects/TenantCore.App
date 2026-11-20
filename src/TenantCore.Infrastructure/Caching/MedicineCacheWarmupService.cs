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
    ILogger<MedicineCacheWarmupService> logger,
    TimeSpan? startupDelay = null) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the first fetch waits, so EF migrations and seeding can finish before it competes for
    /// locks on the Medicines table. Overridable purely so a test does not have to sit through the
    /// production settle window.
    /// </summary>
    private readonly TimeSpan _startupDelay = startupDelay ?? TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let EF migrations, seeding and other startup queries finish before competing for
        // shared locks on the Medicines table.
        try { await Task.Delay(_startupDelay, stoppingToken); }
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

            List<Medicine> systemMedicines;
            List<MedicineType> medicineTypes;
            List<MedicineDosageForm> dosageForms;

            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.SetCommandTimeout(QueryTimeout);

                // Cache warmup is a best-effort snapshot — it never participates in any business
                // transaction and refreshes every 30 minutes, so dirty reads are fine. READ
                // UNCOMMITTED means the queries acquire no shared locks and can never be blocked
                // by a concurrent writer holding a lock on the Medicines table. Using EF Core's
                // transaction API (rather than a raw SET TRANSACTION ISOLATION LEVEL statement)
                // keeps this off the repo's no-raw-SQL rule while preserving the exact same
                // dirty-read behavior for the duration of the three loads below.
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, ct);

                systemMedicines = await MedicineCacheQueries.LoadSystemMedicinesAsync(dbContext, ct);
                medicineTypes = await MedicineCacheQueries.LoadMedicineTypesAsync(dbContext, ct);
                dosageForms = await MedicineCacheQueries.LoadMedicineDosageFormsAsync(dbContext, ct);

                await transaction.CommitAsync(ct);
            }
            else
            {
                systemMedicines = await MedicineCacheQueries.LoadSystemMedicinesAsync(dbContext, ct);
                medicineTypes = await MedicineCacheQueries.LoadMedicineTypesAsync(dbContext, ct);
                dosageForms = await MedicineCacheQueries.LoadMedicineDosageFormsAsync(dbContext, ct);
            }

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
