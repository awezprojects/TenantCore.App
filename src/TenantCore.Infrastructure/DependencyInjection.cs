using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Common;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Tasks;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.BackgroundJobs;
using TenantCore.Infrastructure.Caching;
using TenantCore.Infrastructure.ExternalServices;
using TenantCore.Infrastructure.ExternalServices.Razorpay;
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

        // Clinic onboarding + durable workflow (see plan/clinic-trial-razorpay-subscriptions/PLAN.md)
        services.AddScoped<IClinicOnboardingRequestRepository, ClinicOnboardingRequestRepository>();
        services.AddScoped<ISubscriptionPaymentRepository, SubscriptionPaymentRepository>();
        services.AddScoped<IWorkflowTaskRepository, WorkflowTaskRepository>();
        services.AddScoped<IPaymentWebhookEventRepository, PaymentWebhookEventRepository>();

        services.Configure<RazorpayOptions>(configuration.GetSection("Razorpay"));
        services.Configure<AuthInternalApiOptions>(configuration.GetSection("AuthInternalApi"));

        // Missing Razorpay keys must never fail startup — IPaymentGateway.IsConfigured governs
        // whether payment operations are attempted; the app still runs with payments disabled.
        services.AddHttpClient("RazorpayApi", (sp, client) =>
        {
            var razorpayOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RazorpayOptions>>().Value;
            client.BaseAddress = new Uri(razorpayOptions.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilienceHandler();

        services.AddHttpClient("AuthInternalApi", (sp, client) =>
        {
            var authInternalOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthInternalApiOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(authInternalOptions.BaseUrl)
                ? configuration["AuthApi:BaseUrl"] ?? "https://localhost:7136/"
                : authInternalOptions.BaseUrl;
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilienceHandler();

        services.AddScoped<IPaymentGateway, RazorpayPaymentGateway>();
        services.AddScoped<IAuthProvisioningService, AuthProvisioningService>();

        services.AddHostedService<WorkflowTaskProcessor>();
        services.AddHostedService<PaymentReconciliationService>();

        // Every IWorkflowTaskHandler implementation — dispatched by TaskType at runtime.
        services.AddScoped<IWorkflowTaskHandler, CreatePaymentLinkTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, CancelPaymentLinkTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, SendEmailTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, ConfirmPaymentTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, ProvisionClinicTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, ActivateSubscriptionTaskHandler>();
        services.AddScoped<IWorkflowTaskHandler, ProcessWebhookEventTaskHandler>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfConversionService, PdfConversionService>();
        services.AddScoped<IPrescriptionPdfGenerator, PrescriptionPdfGenerator>();

        services.AddAppLogging(configuration);
        services.AddScoped<IErrorLogger, ErrorLoggingService>();
        services.AddScoped<IActionLogger, ActionLoggingService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IWorkflowCorrelationContext, WorkflowCorrelationContext>();

        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IAuthClinicService, AuthClinicService>();

        services.AddScoped<IApplicationAccessValidator, ApplicationAccessValidator>();

        return services;
    }
}
