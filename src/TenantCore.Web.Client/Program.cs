using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TenantCore.Web.Client;
using TenantCore.Web.Client.Clients;
using TenantCore.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

// Toast notification service
builder.Services.AddSingleton<IToastService, ToastService>();

// Register Auth services
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddSingleton<RoleCacheService>();

// Clinic context (persists selected clinic across pages).
// Must be Singleton — IHttpClientFactory creates a fresh DI scope for each handler pipeline,
// so AddScoped would give ClinicAuthorizationHandler a different ClinicContextService instance
// from the one initialized by layout components, causing the X-Application-Id header to never
// be injected for endpoints that don't pass an explicit applicationId.
builder.Services.AddSingleton<ClinicContextService>();
builder.Services.AddTransient<ClinicAuthorizationHandler>();

// Get API base URLs from configuration
var tenantApiBaseUrl = builder.Configuration["TenantApiBaseUrl"] ?? "https://localhost:7246/";
var authApiBaseUrl = builder.Configuration["AuthApiBaseUrl"] ?? tenantApiBaseUrl;

// Register Auth API Client
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(authApiBaseUrl);
});

// Register Doctor Profile API Client (doctor-level, no clinic context header)
builder.Services.AddHttpClient<IDoctorProfileApiClient, DoctorProfileApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
});

// Register Doctor Specialities API Client
builder.Services.AddHttpClient<IDoctorSpecialitiesApiClient, DoctorSpecialitiesApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
});

// Register Clinic API Client
builder.Services.AddHttpClient<IClinicApiClient, ClinicApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Application API Client
builder.Services.AddHttpClient<IApplicationApiClient, ApplicationApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Medicine API Client
builder.Services.AddHttpClient<IMedicineApiClient, MedicineApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Prescription API Client
builder.Services.AddHttpClient<IPrescriptionApiClient, PrescriptionApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Medicine Bundle API Client
builder.Services.AddHttpClient<IMedicineBundleApiClient, MedicineBundleApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Obstetric API Client
builder.Services.AddHttpClient<IObstetricApiClient, ObstetricApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register USG Template API Client
builder.Services.AddHttpClient<IUsgTemplateApiClient, UsgTemplateApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Pregnancy Tenure API Client
builder.Services.AddHttpClient<IPregnancyTenureApiClient, PregnancyTenureApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Counter & Expenses Management API Clients
builder.Services.AddHttpClient<IDoctorFeeConfigApiClient, DoctorFeeConfigApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IParticularApiClient, ParticularApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IOpdParticularApiClient, OpdParticularApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IOpdPaymentApiClient, OpdPaymentApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IExpenseCategoryApiClient, ExpenseCategoryApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IExpenseRecordApiClient, ExpenseRecordApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<ICounterSessionApiClient, CounterSessionApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IAmountHandoverApiClient, AmountHandoverApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

builder.Services.AddHttpClient<IFinanceReportApiClient, FinanceReportApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();

// Register Subscription API Client + the shared status cache used by
// AuthorizedLayout, the dashboards and the top-bar status pill.
builder.Services.AddHttpClient<ISubscriptionApiClient, SubscriptionApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
}).AddHttpMessageHandler<ClinicAuthorizationHandler>();
builder.Services.AddScoped<SubscriptionContextService>();

await builder.Build().RunAsync();
