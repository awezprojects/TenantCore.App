using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TenantCore.Web.Client;
using TenantCore.Web.Client.Services;
using TenantCore.Web.Client.Clients;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Register Auth services
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<AuthStateService>();

// Get API base URLs from configuration
var tenantApiBaseUrl = builder.Configuration["TenantApiBaseUrl"] ?? "https://localhost:7246/";
var authApiBaseUrl = builder.Configuration["AuthApiBaseUrl"] ?? tenantApiBaseUrl;

// Register Tenant API Client
builder.Services.AddHttpClient<ITenantApiClient, TenantApiClient>(client =>
{
    client.BaseAddress = new Uri(tenantApiBaseUrl);
});

// Register Auth API Client
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(authApiBaseUrl);
});

await builder.Build().RunAsync();
