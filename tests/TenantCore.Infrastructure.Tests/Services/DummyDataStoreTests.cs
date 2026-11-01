using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Infrastructure.Services;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Tests.Services;

/// <summary>
/// The dummy-data store against a real ClinicDbContext (InMemory — its single SaveChanges is atomic,
/// so the store's relational-transaction guard simply skips the explicit transaction here).
///
/// The prefix convention is the whole contract: every assertion below is about what the "DUMMY"
/// marker does and does not catch.
/// </summary>
public class DummyDataStoreTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    private ClinicDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ClinicDbContext>().UseInMemoryDatabase(_databaseName).Options);

    private DummyDataStore Store(ClinicDbContext db) => new(db);

    /// <summary>A clinic that can be seeded completely: one active doctor and two of its own medicines.</summary>
    private static async Task SeedClinicAsync(
        ClinicDbContext db, Guid applicationId, bool withDoctor = true, bool withMedicines = true)
    {
        if (withDoctor)
        {
            var profile = DoctorProfile.Create(Guid.NewGuid(), "REG-1", Guid.NewGuid(), null);
            db.Add(profile);
            db.Add(DoctorFeeConfig.Create(applicationId, profile.Id, 300m));
        }

        if (withMedicines)
        {
            db.Add(Medicine.Create(
                name: "Paracetamol", genericName: null, brandName: null, description: null,
                composition: null, composition2: null, dosage: null, form: "Tab", manufacturer: null,
                isGeneric: false, packSize: null, uses: null, sideEffects: null, contraindications: null,
                storage: null, medicineTypeId: null, applicationId: applicationId));

            db.Add(Medicine.Create(
                name: "Amoxicillin", genericName: null, brandName: null, description: null,
                composition: null, composition2: null, dosage: null, form: "Capsule", manufacturer: null,
                isGeneric: false, packSize: null, uses: null, sideEffects: null, contraindications: null,
                storage: null, medicineTypeId: null, applicationId: applicationId));
        }

        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- status

    [Fact]
    public async Task GetStatus_EmptyClinic_IsAllZeroes()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();

        var status = await Store(db).GetStatusAsync(applicationId);

        status.HasDummyData.Should().BeFalse();
        status.PatientCount.Should().Be(0);
        status.WardCount.Should().Be(0);
        status.OpdRegistrationsSkipped.Should().BeFalse();
        status.SkipReason.Should().BeNull();
    }

    [Fact]
    public async Task GetStatus_NeverCountsAnotherClinicsDummyData()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        await using (var db = CreateDb())
        {
            await SeedClinicAsync(db, clinicA);
            await Store(db).SeedAsync(clinicA);
        }

        await using (var db = CreateDb())
        {
            var status = await Store(db).GetStatusAsync(clinicB);

            status.PatientCount.Should().Be(0);
            status.WardCount.Should().Be(0);
            status.HasDummyData.Should().BeFalse();
        }
    }

    // ---------------------------------------------------------------- seed

    [Fact]
    public async Task Seed_CreatesTheCanonicalSet()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);

        var status = await Store(db).SeedAsync(applicationId);

        status.PatientCount.Should().Be(75);
        status.WardCount.Should().Be(3);
        status.RoomCount.Should().Be(15);
        status.BedCount.Should().Be(31);
        status.ParticularCount.Should().Be(8);
        status.OpdRegistrationCount.Should().Be(75);
        status.PrescriptionCount.Should().Be(75);
        status.IpdRegistrationCount.Should().Be(0, "IPD admissions are made by hand during testing");
        status.HasDummyData.Should().BeTrue();
        status.OpdRegistrationsSkipped.Should().BeFalse();
        status.PrescriptionsSkipped.Should().BeFalse();
    }

    [Fact]
    public async Task Seed_AppliesThePrefixConventionToEveryRecord()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);

        await Store(db).SeedAsync(applicationId);

        (await db.Patients.ToListAsync()).Should()
            .OnlyContain(p => p.FirstName == "DUMMY" && p.PhoneNumber == "0000000000");
        (await db.Wards.ToListAsync()).Should().OnlyContain(w => w.Name.StartsWith("DUMMY "));
        (await db.Rooms.ToListAsync()).Should().OnlyContain(r => r.RoomNumber.StartsWith("DUMMY-"));
        (await db.Beds.ToListAsync()).Should().OnlyContain(b => b.BedNumber.StartsWith("DUMMY-"));
        (await db.Particulars.ToListAsync()).Should().OnlyContain(p => p.Name.StartsWith("DUMMY "));
    }

    [Fact]
    public async Task Seed_TagsEveryRecordWithTheRequestedClinic()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);

        await Store(db).SeedAsync(applicationId);

        (await db.Patients.ToListAsync()).Should().OnlyContain(p => p.ApplicationId == applicationId);
        (await db.Wards.ToListAsync()).Should().OnlyContain(w => w.ApplicationId == applicationId);
        (await db.Rooms.ToListAsync()).Should().OnlyContain(r => r.ApplicationId == applicationId);
        (await db.Beds.ToListAsync()).Should().OnlyContain(b => b.ApplicationId == applicationId);
        (await db.OpdRegistrations.ToListAsync()).Should().OnlyContain(r => r.ApplicationId == applicationId);
        (await db.Prescriptions.ToListAsync()).Should().OnlyContain(p => p.ApplicationId == applicationId);
        (await db.Particulars.ToListAsync()).Should().OnlyContain(p => p.ApplicationId == applicationId);
    }

    [Fact]
    public async Task Seed_WithoutAnActiveDoctor_SkipsVisitsAndSaysWhy()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId, withDoctor: false);

        var status = await Store(db).SeedAsync(applicationId);

        status.PatientCount.Should().Be(75, "the infrastructure is still worth seeding");
        status.WardCount.Should().Be(3);
        status.OpdRegistrationCount.Should().Be(0);
        status.PrescriptionCount.Should().Be(0);
        status.OpdRegistrationsSkipped.Should().BeTrue();
        status.SkipReason.Should().Contain("doctor");
        (await db.OpdRegistrations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_WithoutAnyClinicMedicine_SkipsOnlyPrescriptions()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId, withMedicines: false);

        var status = await Store(db).SeedAsync(applicationId);

        status.OpdRegistrationCount.Should().Be(75);
        status.PrescriptionCount.Should().Be(0);
        status.PrescriptionsSkipped.Should().BeTrue();
        status.OpdRegistrationsSkipped.Should().BeFalse();
    }

    [Fact]
    public async Task Seed_UsesTheClinicsDoctorAndItsOwnMedicines()
    {
        var applicationId = Guid.NewGuid();
        var doctorUserId = Guid.NewGuid();
        await using var db = CreateDb();

        var profile = DoctorProfile.Create(doctorUserId, "REG-1", Guid.NewGuid(), null);
        db.Add(profile);
        db.Add(DoctorFeeConfig.Create(applicationId, profile.Id, 300m));
        db.Add(Medicine.Create(
            name: "Paracetamol", genericName: null, brandName: null, description: null,
            composition: null, composition2: null, dosage: null, form: "Tab", manufacturer: null,
            isGeneric: false, packSize: null, uses: null, sideEffects: null, contraindications: null,
            storage: null, medicineTypeId: null, applicationId: applicationId));
        await db.SaveChangesAsync();

        await Store(db).SeedAsync(applicationId);

        (await db.OpdRegistrations.ToListAsync()).Should().OnlyContain(r => r.DoctorUserId == doctorUserId);
        (await db.PrescriptionItems.ToListAsync()).Should()
            .OnlyContain(i => i.MedicineName == "Paracetamol" && i.MedicineForm == MedicineFormType.Tab);
    }

    [Fact]
    public async Task Seed_AlreadySeeded_ThrowsAndChangesNothing()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);

        await Store(db).SeedAsync(applicationId);
        var patientsBefore = await db.Patients.CountAsync();

        var act = async () => await Store(db).SeedAsync(applicationId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already has dummy data*");
        (await db.Patients.CountAsync()).Should().Be(patientsBefore);
    }

    // ---------------------------------------------------------------- purge

    [Fact]
    public async Task Purge_RemovesEveryDummyRecord()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);
        await Store(db).SeedAsync(applicationId);

        var status = await Store(db).PurgeAsync(applicationId);

        status.HasDummyData.Should().BeFalse();
        status.PatientCount.Should().Be(0);
        status.WardCount.Should().Be(0);
        status.RoomCount.Should().Be(0);
        status.BedCount.Should().Be(0);
        status.ParticularCount.Should().Be(0);
        status.OpdRegistrationCount.Should().Be(0);
        status.PrescriptionCount.Should().Be(0);
        status.IpdRegistrationCount.Should().Be(0);

        (await db.Patients.CountAsync()).Should().Be(0);
        (await db.Wards.CountAsync()).Should().Be(0);
        (await db.Rooms.CountAsync()).Should().Be(0);
        (await db.Beds.CountAsync()).Should().Be(0);
        (await db.Particulars.CountAsync()).Should().Be(0);
        (await db.OpdRegistrations.CountAsync()).Should().Be(0);
        (await db.Prescriptions.CountAsync()).Should().Be(0);
        (await db.PrescriptionItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Purge_LeavesRecordsThatAreNotDummyDataAlone()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);
        await Store(db).SeedAsync(applicationId);

        // Sitting next to the seeded set: records the clinic team created themselves.
        var realPatient = Patient.Create(
            applicationId, "Asha", "Rao", null, Gender.Female, "9999999999",
            email: null, aadhaarNumber: null, photoUrl: null, address: null);
        var realWard = Ward.Create(applicationId, "Maternity", null);
        var realParticular = Particular.Create(applicationId, "Consultation", 250m);
        db.AddRange(realPatient, realWard, realParticular);
        await db.SaveChangesAsync();

        await Store(db).PurgeAsync(applicationId);

        (await db.Patients.ToListAsync()).Should().ContainSingle().Which.Id.Should().Be(realPatient.Id);
        (await db.Wards.ToListAsync()).Should().ContainSingle().Which.Id.Should().Be(realWard.Id);
        (await db.Particulars.ToListAsync()).Should().ContainSingle().Which.Id.Should().Be(realParticular.Id);
    }

    [Fact]
    public async Task Purge_CleansChildrenATesterAddedToADummyVisit()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);
        await Store(db).SeedAsync(applicationId);

        var dummyPatient = await db.Patients.FirstAsync();
        var dummyVisit = await db.OpdRegistrations.FirstAsync();

        // A payment, a service item and an IPD admission a tester added by hand to the dummy records.
        db.Add(OpdPayment.Create(applicationId, dummyVisit.Id, 300m));
        db.Add(OpdParticular.Create(applicationId, dummyVisit.Id, Guid.NewGuid(), "X-Ray", 200m));
        db.Add(IpdRegistration.Create(
            applicationId, dummyPatient.Id, dummyVisit.DoctorUserId, "DUMMY Doctor", "DUMMY-IPD-1",
            "DUMMY General Ward", "DUMMY-G-101", "DUMMY-1", 500m, null));
        await db.SaveChangesAsync();

        await Store(db).PurgeAsync(applicationId);

        (await db.OpdPayments.CountAsync()).Should().Be(0);
        (await db.OpdParticulars.CountAsync()).Should().Be(0);
        (await db.IpdRegistrations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Purge_NeverTouchesAnotherClinicsDummyData()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        await using var db = CreateDb();

        await SeedClinicAsync(db, clinicA);
        await SeedClinicAsync(db, clinicB);
        await Store(db).SeedAsync(clinicA);
        await Store(db).SeedAsync(clinicB);

        await Store(db).PurgeAsync(clinicA);

        var clinicBStatus = await Store(db).GetStatusAsync(clinicB);
        clinicBStatus.PatientCount.Should().Be(75);
        clinicBStatus.WardCount.Should().Be(3);
    }

    [Fact]
    public async Task Purge_NothingToPurge_Throws()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();

        var act = async () => await Store(db).PurgeAsync(applicationId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no dummy data*");
    }

    [Fact]
    public async Task Purge_ThenSeed_WorksAgain()
    {
        var applicationId = Guid.NewGuid();
        await using var db = CreateDb();
        await SeedClinicAsync(db, applicationId);

        await Store(db).SeedAsync(applicationId);
        await Store(db).PurgeAsync(applicationId);
        var status = await Store(db).SeedAsync(applicationId);

        status.PatientCount.Should().Be(75);
        status.BedCount.Should().Be(31);
    }
}
