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
    public DbSet<MedicineDosageForm> MedicineDosageForms => Set<MedicineDosageForm>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionReport> PrescriptionReports => Set<PrescriptionReport>();
    public DbSet<DosageRemark> DosageRemarks => Set<DosageRemark>();
    public DbSet<PrescriptionConfig> PrescriptionConfigs => Set<PrescriptionConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);
    }
}
