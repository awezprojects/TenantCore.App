using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TenantCore.Web.Client;
using TenantCore.Web.Client.Clients;
using TenantCore.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Register Auth services
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddSingleton<RoleCacheService>();

// Clinic context (persists selected clinic across pages)
builder.Services.AddScoped<ClinicContextService>();
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

await builder.Build().RunAsync();
