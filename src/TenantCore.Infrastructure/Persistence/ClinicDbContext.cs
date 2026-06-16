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
    public DbSet<ObstetricPrescriptionData> ObstetricPrescriptionData => Set<ObstetricPrescriptionData>();
    public DbSet<DosageRemark> DosageRemarks => Set<DosageRemark>();
    public DbSet<PrescriptionConfig> PrescriptionConfigs => Set<PrescriptionConfig>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<DoctorSpeciality> DoctorSpecialities => Set<DoctorSpeciality>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<ClinicUsgTemplate> ClinicUsgTemplates => Set<ClinicUsgTemplate>();
    public DbSet<UsgTemplateRow> UsgTemplateRows => Set<UsgTemplateRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);
    }
}
