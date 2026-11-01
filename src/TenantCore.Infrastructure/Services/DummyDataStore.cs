using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TenantCore.Application.Common;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Persistence;
using TenantCore.Shared.Dtos.PlatformAdmin;
using TenantCore.Shared.Enums;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// The dummy-data tool's data layer — everything the Admin portal's Test Data page can do to a clinic.
///
/// There is no IsDummyRecord column and no tracking table: the "DUMMY" prefix convention IS the
/// identifier. Both writes run inside one transaction, so a clinic is never left half-seeded or
/// half-purged.
///
/// Seeding bypasses the CQRS pipeline on purpose. The normal CreateOpdRegistrationCommand needs an
/// active counter session, a doctor's JWT and fee validation — none of which a service-to-service
/// caller has — and it would also charge platform usage for records that are pure test data. The
/// DummyData:Enabled guard on the controller is what keeps this path out of production.
/// </summary>
public sealed class DummyDataStore(ClinicDbContext db) : IDummyDataStore
{
    // ---- the prefix convention ----------------------------------------------------------------
    private const string DummyFirstName = "DUMMY";
    private const string DummyPhoneNumber = "0000000000";

    /// <summary>Wards and particulars are named "DUMMY &lt;label&gt;".</summary>
    private const string DummyNamePrefix = "DUMMY ";

    /// <summary>Rooms and beds are numbered "DUMMY-&lt;code&gt;".</summary>
    private const string DummyRoomPrefix = "DUMMY-";

    private const string SkipReasonNoDoctor = "No doctor profile in clinic";

    /// <summary>
    /// DoctorProfile carries no display name — the App normally takes DoctorName from the booking
    /// client. A service-to-service seeder has no such input, so the seeded visits carry a marker.
    /// </summary>
    private const string DummyDoctorName = "DUMMY Doctor";

    private const int PatientTarget = 75;
    private const int YoungestAge = 18;
    private const int OldestAge = 75;

    /// <summary>How many of the clinic's own medicines a seeded prescription draws on (plan: 2–3).</summary>
    private const int MedicinesPerPrescription = 3;

    private const decimal ConsultationFee = 300m;

    /// <summary>The 8 service items. OPD and IPD particulars share one table — the split is semantic.</summary>
    private static readonly (string Name, decimal DefaultAmount)[] ParticularTemplates =
    [
        ($"{DummyNamePrefix}Consultation Fee", 300m),
        ($"{DummyNamePrefix}Dressing Charge", 150m),
        ($"{DummyNamePrefix}USG Charge", 500m),
        ($"{DummyNamePrefix}ECG Charge", 200m),
        ($"{DummyNamePrefix}Operation Theatre", 5000m),
        ($"{DummyNamePrefix}Lab Charge", 800m),
        ($"{DummyNamePrefix}Medicine Charge", 1000m),
        ($"{DummyNamePrefix}Nursing Charge", 400m),
    ];

    /// <summary>The 3 wards — one per distinct ward in <see cref="RoomTemplates"/>.</summary>
    private static readonly (string Name, string Description)[] WardTemplates =
    [
        ($"{DummyNamePrefix}General Ward", "Standard general beds, multi-bed rooms"),
        ($"{DummyNamePrefix}Private Ward", "Private and semi-private rooms"),
        ($"{DummyNamePrefix}ICU", "Intensive care, single-bed rooms"),
    ];

    /// <summary>
    /// The canonical 15 rooms — 6 general (3 beds each), 4 semi-private (2), 2 private (1), 3 ICU (1)
    /// = 31 beds. Room numbers restart per room for beds: DUMMY-1, DUMMY-2, …
    /// </summary>
    private static readonly (string WardName, string RoomNumber, string RoomType, decimal PricePerDay, int Beds)[] RoomTemplates =
    [
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-101", "General", 300m, 3),
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-102", "General", 300m, 3),
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-103", "General", 300m, 3),
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-104", "General", 300m, 3),
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-105", "General", 300m, 3),
        ($"{DummyNamePrefix}General Ward", "DUMMY-G-106", "General", 300m, 3),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-201", "Semi-Private", 600m, 2),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-202", "Semi-Private", 600m, 2),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-203", "Semi-Private", 600m, 2),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-204", "Semi-Private", 600m, 2),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-205", "Private", 1200m, 1),
        ($"{DummyNamePrefix}Private Ward", "DUMMY-P-206", "Private", 1200m, 1),
        ($"{DummyNamePrefix}ICU", "DUMMY-I-301", "ICU", 2500m, 1),
        ($"{DummyNamePrefix}ICU", "DUMMY-I-302", "ICU", 2500m, 1),
        ($"{DummyNamePrefix}ICU", "DUMMY-I-303", "ICU", 2500m, 1),
    ];

    // ---- reads ---------------------------------------------------------------------------------

    public Task<DummyDataStatusDto> GetStatusAsync(Guid applicationId, CancellationToken ct = default)
        => BuildStatusAsync(applicationId, ct);

    /// <summary>
    /// Counts by prefix. Ids are collected where the counts need them anyway, so no table is read twice.
    ///
    /// The two "Skipped" flags are derived rather than remembered: a seed is atomic, but a tester can
    /// delete a doctor or the last medicine afterwards, and the page must then tell the truth about
    /// why a count is zero.
    /// </summary>
    private async Task<DummyDataStatusDto> BuildStatusAsync(Guid applicationId, CancellationToken ct)
    {
        var patientIds = await DummyPatientIdsAsync(applicationId, ct);

        var wardIds = await db.Wards
            .Where(w => w.ApplicationId == applicationId && w.Name.StartsWith(DummyNamePrefix))
            .Select(w => w.Id)
            .ToListAsync(ct);

        var roomIds = await db.Rooms
            .Where(r => r.ApplicationId == applicationId && r.RoomNumber.StartsWith(DummyRoomPrefix))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var bedCount = await db.Beds
            .CountAsync(b => b.ApplicationId == applicationId && roomIds.Contains(b.RoomId), ct);

        var particularCount = await db.Particulars
            .CountAsync(p => p.ApplicationId == applicationId && p.Name.StartsWith(DummyNamePrefix), ct);

        var opdIds = await DummyOpdIdsAsync(applicationId, patientIds, ct);

        var prescriptionCount = await db.Prescriptions
            .CountAsync(p => p.ApplicationId == applicationId && opdIds.Contains(p.OpdRegistrationId), ct);

        // IPD admissions are made by hand on a dummy patient during testing; the seed never creates one.
        var ipdCount = await db.IpdRegistrations
            .CountAsync(r => r.ApplicationId == applicationId && patientIds.Contains(r.PatientId), ct);

        var opdSkipped = patientIds.Count > 0 && opdIds.Count == 0;

        return new DummyDataStatusDto
        {
            HasDummyData = patientIds.Count > 0 || wardIds.Count > 0 || particularCount > 0
                           || opdIds.Count > 0 || ipdCount > 0,
            PatientCount = patientIds.Count,
            WardCount = wardIds.Count,
            RoomCount = roomIds.Count,
            BedCount = bedCount,
            ParticularCount = particularCount,
            OpdRegistrationCount = opdIds.Count,
            PrescriptionCount = prescriptionCount,
            IpdRegistrationCount = ipdCount,
            OpdRegistrationsSkipped = opdSkipped,
            PrescriptionsSkipped = opdIds.Count > 0 && prescriptionCount == 0,
            SkipReason = opdSkipped ? SkipReasonNoDoctor : null,
        };
    }

    private Task<List<Guid>> DummyPatientIdsAsync(Guid applicationId, CancellationToken ct) =>
        db.Patients
            .Where(p => p.ApplicationId == applicationId
                        && p.FirstName == DummyFirstName
                        && p.PhoneNumber == DummyPhoneNumber)
            .Select(p => p.Id)
            .ToListAsync(ct);

    private Task<List<Guid>> DummyOpdIdsAsync(Guid applicationId, IReadOnlyList<Guid> patientIds, CancellationToken ct) =>
        db.OpdRegistrations
            .Where(r => r.ApplicationId == applicationId && patientIds.Contains(r.PatientId))
            .Select(r => r.Id)
            .ToListAsync(ct);

    // ---- seed ---------------------------------------------------------------------------------

    public async Task<DummyDataStatusDto> SeedAsync(Guid applicationId, CancellationToken ct = default)
    {
        await using var transaction = await BeginTransactionAsync(ct);

        var before = await BuildStatusAsync(applicationId, ct);
        if (before.HasDummyData)
            throw new InvalidOperationException(
                "This clinic already has dummy data. Purge it before seeding a fresh set.");

        var wards = WardTemplates
            .Select(w => Ward.Create(applicationId, w.Name, w.Description))
            .ToList();

        var wardsByName = wards.ToDictionary(w => w.Name, w => w);
        var rooms = new List<Room>();
        var beds = new List<Bed>();

        foreach (var template in RoomTemplates)
        {
            var ward = wardsByName[template.WardName];
            var room = Room.Create(applicationId, ward.Id, template.RoomNumber, template.RoomType, template.PricePerDay);
            rooms.Add(room);

            for (var i = 1; i <= template.Beds; i++)
                beds.Add(Bed.Create(applicationId, ward.Id, room.Id, $"{DummyRoomPrefix}{i}"));
        }

        var particulars = ParticularTemplates
            .Select(p => Particular.Create(applicationId, p.Name, p.DefaultAmount))
            .ToList();

        var patients = Enumerable.Range(1, PatientTarget)
            .Select(index => CreatePatient(applicationId, index))
            .ToList();

        db.AddRange(wards);
        db.AddRange(rooms);
        db.AddRange(beds);
        db.AddRange(particulars);
        db.AddRange(patients);

        await SeedVisitsAsync(applicationId, patients, ct);

        await db.SaveChangesAsync(ct);

        if (transaction is not null)
            await transaction.CommitAsync(ct);

        return await BuildStatusAsync(applicationId, ct);
    }

    /// <summary>
    /// Anonymous but realistic: FirstName plus the shared phone number is the dummy marker. Ages are
    /// spread across 18–75 by index rather than randomly, so seeding the same clinic twice produces
    /// the same rows and a test can assert on one of them.
    /// </summary>
    private static Patient CreatePatient(Guid applicationId, int index)
    {
        var age = YoungestAge + (index % (OldestAge - YoungestAge + 1));

        return Patient.Create(
            applicationId,
            DummyFirstName,
            $"Patient {index:D3}",
            DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-age),
            index % 2 == 0 ? Gender.Female : Gender.Male,
            DummyPhoneNumber,
            email: null,
            aadhaarNumber: null,
            photoUrl: null,
            address: null);
    }

    /// <summary>
    /// One transaction for the whole write. The relational guard exists for the InMemory provider the
    /// unit tests use — it has no transactions, and a single SaveChanges is already atomic there.
    /// </summary>
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct) =>
        db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;

    /// <summary>
    /// The clinic's first ACTIVE doctor. DoctorProfile is deliberately not tenant-scoped — a doctor
    /// can practise at several clinics and carries no ApplicationId — so the clinic→doctor link is
    /// DoctorFeeConfig, the per-clinic fee row the App already uses to price a visit.
    /// </summary>
    private async Task<Guid?> FirstDoctorUserIdAsync(Guid applicationId, CancellationToken ct) =>
        await (from fee in db.DoctorFeeConfigs
               where fee.ApplicationId == applicationId && fee.IsActive
               join profile in db.DoctorProfiles on fee.DoctorProfileId equals profile.Id
               orderby fee.CreatedAt
               select (Guid?)profile.UserId)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// One OPD visit per patient and one prescription per visit, created directly rather than through
    /// the CQRS pipeline — there is no counter session to satisfy, no doctor JWT to read and nothing
    /// to bill for pure test data.
    ///
    /// Both steps degrade instead of failing: no active doctor means no visits, no clinic medicine
    /// means no prescriptions. The status returned to the portal says which happened.
    /// </summary>
    private async Task SeedVisitsAsync(Guid applicationId, IReadOnlyList<Patient> patients, CancellationToken ct)
    {
        var doctorUserId = await FirstDoctorUserIdAsync(applicationId, ct);
        if (doctorUserId is null)
            return;

        var medicines = await db.Medicines
            .Where(m => m.ApplicationId == applicationId && m.IsActive)
            .OrderBy(m => m.Name)
            .Take(MedicinesPerPrescription)
            .ToListAsync(ct);

        var registrations = new List<OpdRegistration>();
        var prescriptions = new List<Prescription>();
        var items = new List<PrescriptionItem>();

        for (var i = 0; i < patients.Count; i++)
        {
            var patient = patients[i];

            var registration = OpdRegistration.Create(
                applicationId, patient.Id, doctorUserId.Value, DummyDoctorName,
                $"DUMMY-OPD-{i + 1:D3}", ConsultationFee, notes: null);
            registrations.Add(registration);

            if (medicines.Count == 0)
                continue;

            // Create assigns Id before returning, so the items can point at the prescription.
            var prescription = Prescription.Create(
                applicationId, registration.Id, patient.Id, doctorUserId.Value, DummyDoctorName,
                $"DUMMY-RX-{i + 1:D3}",
                nextVisitDate: null, diagnosis: null, investigations: null, notes: null,
                vitalBP: null, vitalPulse: null, vitalTemp: null, vitalWeight: null, vitalSpO2: null,
                vitalRR: null, vitalSugar: null,
                items: []);

            // Update is the only way to attach items — there is no AddItem on the aggregate.
            prescription.Update(
                nextVisitDate: null, diagnosis: null, investigations: null, notes: null,
                vitalBP: null, vitalPulse: null, vitalTemp: null, vitalWeight: null, vitalSpO2: null,
                vitalRR: null, vitalSugar: null,
                items: medicines.Select((medicine, index) => CreateItem(prescription.Id, medicine, index)));

            prescriptions.Add(prescription);
            items.AddRange(prescription.Items);
        }

        db.AddRange(registrations);
        db.AddRange(prescriptions);
        db.AddRange(items);
    }

    private static PrescriptionItem CreateItem(Guid prescriptionId, Medicine medicine, int sortOrder) =>
        PrescriptionItem.Create(
            prescriptionId,
            medicine.Id,
            medicine.Name,
            FormOf(medicine.Form),
            strength: null,
            dosageUnit: "Tab",
            dosageMorning: 1,
            dosageAfternoon: null,
            dosageEvening: null,
            dosageNight: 1,
            durationDays: 3,
            quantity: 6,
            frequency: "1-0-1",
            timing: "After Food",
            instructions: null,
            remarkEnglish: null,
            remarkHindi: null,
            remarkMarathi: null,
            sortOrder);

    /// <summary>Medicine.Form is free text; a prescription item wants the enum. Anything unknown is Other.</summary>
    private static MedicineFormType FormOf(string? form) =>
        Enum.TryParse<MedicineFormType>(form, ignoreCase: true, out var parsed) ? parsed : MedicineFormType.Other;

    // ---- purge --------------------------------------------------------------------------------

    public async Task<DummyDataStatusDto> PurgeAsync(Guid applicationId, CancellationToken ct = default)
    {
        await using var transaction = await BeginTransactionAsync(ct);

        var before = await BuildStatusAsync(applicationId, ct);
        if (!before.HasDummyData)
            throw new InvalidOperationException("This clinic has no dummy data to purge.");

        // Deleted leaves-first, along the FK tree, all in one SaveChanges inside one transaction: an
        // unexpected FK can only roll the whole purge back, never half-apply it.
        //
        // IpdRegistration has NO child tables in the model today — nothing anywhere in ClinicDbContext
        // carries an IpdRegistrationId (verified when this was written). A tester admits dummy patients
        // by hand, so those rows are removed here without a child pass. If a child table is ever added,
        // it must be deleted BEFORE the parent below.
        var patientIds = await DummyPatientIdsAsync(applicationId, ct);
        var opdIds = await DummyOpdIdsAsync(applicationId, patientIds, ct);

        var prescriptions = await db.Prescriptions
            .Where(p => p.ApplicationId == applicationId && opdIds.Contains(p.OpdRegistrationId))
            .ToListAsync(ct);
        var prescriptionIds = prescriptions.Select(p => p.Id).ToList();

        await RemoveRangeAsync(db.PrescriptionItems.Where(x => prescriptionIds.Contains(x.PrescriptionId)), ct);
        await RemoveRangeAsync(db.PrescriptionReports.Where(x => prescriptionIds.Contains(x.PrescriptionId)), ct);
        db.RemoveRange(prescriptions);

        // OpdParticular and OpdPayment rows are not created by the seed, but a tester may have added
        // them to a dummy visit from the clinic app — they are identified through their parent.
        await RemoveRangeAsync(db.OpdParticulars.Where(x => opdIds.Contains(x.OpdRegistrationId)), ct);
        await RemoveRangeAsync(db.OpdPayments.Where(x => opdIds.Contains(x.OpdRegistrationId)), ct);
        await RemoveRangeAsync(db.OpdRegistrations.Where(x => opdIds.Contains(x.Id)), ct);

        await RemoveRangeAsync(db.IpdRegistrations.Where(x => patientIds.Contains(x.PatientId)), ct);
        await RemoveRangeAsync(db.Patients.Where(x => patientIds.Contains(x.Id)), ct);

        // Beds are found through their room, so the dummy room ids are collected first.
        var roomIds = await db.Rooms
            .Where(r => r.ApplicationId == applicationId && r.RoomNumber.StartsWith(DummyRoomPrefix))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var wardIds = await db.Wards
            .Where(w => w.ApplicationId == applicationId && w.Name.StartsWith(DummyNamePrefix))
            .Select(w => w.Id)
            .ToListAsync(ct);

        await RemoveRangeAsync(db.Beds.Where(x => roomIds.Contains(x.RoomId)), ct);
        await RemoveRangeAsync(db.Rooms.Where(x => roomIds.Contains(x.Id)), ct);
        await RemoveRangeAsync(db.Wards.Where(x => wardIds.Contains(x.Id)), ct);
        await RemoveRangeAsync(
            db.Particulars.Where(x => x.ApplicationId == applicationId && x.Name.StartsWith(DummyNamePrefix)), ct);

        await db.SaveChangesAsync(ct);

        if (transaction is not null)
            await transaction.CommitAsync(ct);

        // Every count is zero now — the portal renders that straight away.
        return await BuildStatusAsync(applicationId, ct);
    }

    /// <summary>
    /// Loads then removes. ExecuteDelete would be one statement cheaper, but this keeps the change
    /// tracker consistent for the SaveChanges that follows, and the sets are small by construction.
    /// </summary>
    private async Task RemoveRangeAsync<T>(IQueryable<T> query, CancellationToken ct) where T : class
    {
        db.RemoveRange(await query.ToListAsync(ct));
    }
}
