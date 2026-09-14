using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Common;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.ExternalServices;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;
using TenantCore.Infrastructure.Services;
using TenantCore.Logging;

namespace TenantCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ClinicDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ClinicConnection"),
                b => b.MigrationsAssembly(typeof(ClinicDbContext).Assembly.FullName)
                      .MigrationsHistoryTable("__EFMigrationsHistory", "clinic")));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IOpdRegistrationRepository, OpdRegistrationRepository>();
        services.AddScoped<IIpdRegistrationRepository, IpdRegistrationRepository>();
        services.AddScoped<IClinicFeeConfigRepository, ClinicFeeConfigRepository>();

        // Medicines, medicine types and dosage forms are served through caching decorators backed
        // by RefreshableCache<T> snapshots that a background hosted service keeps warm (initial
        // fetch on startup, then every 30 minutes) — no live request ever populates or rebuilds
        // them. See plan/medicine-search-caching/PLAN.md.
        services.AddSingleton<RefreshableCache<Medicine>>();
        services.AddSingleton<RefreshableCache<MedicineType>>();
        services.AddSingleton<RefreshableCache<MedicineDosageForm>>();
        services.AddHostedService<MedicineCacheWarmupService>();

        services.AddScoped<MedicineTypeRepository>();
        services.AddScoped<IMedicineTypeRepository, CachedMedicineTypeRepository>();
        services.AddScoped<MedicineDosageFormRepository>();
        services.AddScoped<IMedicineDosageFormRepository, CachedMedicineDosageFormRepository>();
        services.AddScoped<MedicineRepository>();
        services.AddScoped<IMedicineRepository, CachedMedicineRepository>();

        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IMedicineBundleRepository, MedicineBundleRepository>();
        services.AddScoped<IObstetricPrescriptionDataRepository, ObstetricPrescriptionDataRepository>();
        services.AddScoped<IDosageRemarkRepository, DosageRemarkRepository>();
        services.AddScoped<IPrescriptionConfigRepository, PrescriptionConfigRepository>();
        services.AddScoped<IDoctorProfileRepository, DoctorProfileRepository>();
        services.AddScoped<IDoctorSpecialityRepository, DoctorSpecialityRepository>();
        services.AddScoped<IWardRepository, WardRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBedRepository, BedRepository>();
        services.AddScoped<IClinicUsgTemplateRepository, ClinicUsgTemplateRepository>();
        services.AddScoped<IPregnancyTenureRepository, PregnancyTenureRepository>();
        services.AddScoped<IDoctorFeeConfigRepository, DoctorFeeConfigRepository>();
        services.AddScoped<IParticularRepository, ParticularRepository>();
        services.AddScoped<IOpdParticularRepository, OpdParticularRepository>();
        services.AddScoped<IOpdPaymentRepository, OpdPaymentRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseRecordRepository, ExpenseRecordRepository>();
        services.AddScoped<ICounterSessionRepository, CounterSessionRepository>();
        services.AddScoped<IAmountHandoverRepository, AmountHandoverRepository>();
        services.AddScoped<IClinicFeatureFlagsRepository, ClinicFeatureFlagsRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IClinicSubscriptionRepository, ClinicSubscriptionRepository>();
        services.AddScoped<ISubscriptionAlertSettingRepository, SubscriptionAlertSettingRepository>();
        services.AddScoped<IHistoryLookupItemRepository, HistoryLookupItemRepository>();
        services.AddScoped<IVitalPresetLookupItemRepository, VitalPresetLookupItemRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IClinicLocationRepository, ClinicLocationRepository>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfConversionService, PdfConversionService>();
        services.AddScoped<IPrescriptionPdfGenerator, PrescriptionPdfGenerator>();

        services.AddAppLogging(configuration);
        services.AddScoped<IErrorLogger, ErrorLoggingService>();
        services.AddScoped<IActionLogger, ActionLoggingService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IAuthClinicService, AuthClinicService>();

        services.AddScoped<IApplicationAccessValidator, ApplicationAccessValidator>();

        return services;
    }
}
