using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;

namespace TenantCore.Infrastructure.Persistence;

public class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<OpdRegistration> OpdRegistrations => Set<OpdRegistration>();
    public DbSet<IpdRegistration> IpdRegistrations => Set<IpdRegistration>();
    public DbSet<ClinicFeeConfig> ClinicFeeConfigs => Set<ClinicFeeConfig>();
    public DbSet<MedicineType> MedicineTypes => Set<MedicineType>();
    public DbSet<Medicine> Medicines => Set<Medicine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);
    }
}
