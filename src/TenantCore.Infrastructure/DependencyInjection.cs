using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantCore.Application.Common;
using TenantCore.Application.Services;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.ExternalServices;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Repositories;
using TenantCore.Infrastructure.Services;

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
        services.AddScoped<IMedicineTypeRepository, MedicineTypeRepository>();
        services.AddScoped<IMedicineDosageFormRepository, MedicineDosageFormRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
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

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfConversionService, PdfConversionService>();

        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IAuthClinicService, AuthClinicService>();

        services.AddScoped<IApplicationAccessValidator, ApplicationAccessValidator>();

        return services;
    }
}
