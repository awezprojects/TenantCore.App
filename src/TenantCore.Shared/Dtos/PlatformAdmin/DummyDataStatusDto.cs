namespace TenantCore.Shared.Dtos.PlatformAdmin;

/// <summary>
/// The dummy-data state of one clinic, returned by every api/internal/clinics/{id}/dummy-data route.
/// Counts come from the "DUMMY" prefix convention — there is no IsDummyRecord column and no tracking
/// table, so the prefix IS the identifier.
///
/// Mirrored by TenantCore.Admin's DummyDataStatusView: the property names are the JSON contract.
/// </summary>
public sealed record DummyDataStatusDto
{
    /// <summary>True when the clinic holds any dummy record (patients, wards or particulars).</summary>
    public bool HasDummyData { get; init; }

    public int PatientCount { get; init; }
    public int WardCount { get; init; }
    public int RoomCount { get; init; }
    public int BedCount { get; init; }
    public int ParticularCount { get; init; }
    public int OpdRegistrationCount { get; init; }
    public int PrescriptionCount { get; init; }

    /// <summary>IPD admissions a tester created by hand on a dummy patient. The seed never creates these.</summary>
    public int IpdRegistrationCount { get; init; }

    /// <summary>True when patients exist but no OPD registration could be created (no doctor profile).</summary>
    public bool OpdRegistrationsSkipped { get; init; }

    /// <summary>True when OPD registrations exist but no prescription could be created (no medicines).</summary>
    public bool PrescriptionsSkipped { get; init; }

    public string? SkipReason { get; init; }
}
