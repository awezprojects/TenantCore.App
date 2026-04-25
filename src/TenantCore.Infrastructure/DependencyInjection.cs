using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Services;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.ExternalServices;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;

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

        services.AddScoped<IAuthApplicationService, AuthApplicationService>();

        return services;
    }
}
