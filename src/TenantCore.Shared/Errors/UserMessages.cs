namespace TenantCore.Shared.Errors;

/// <summary>
/// User-facing error messages shown in the UI.
/// Keep wording non-technical — no IDs, routes, or stack details.
/// Technical context belongs in server logs (added when logging is introduced).
/// </summary>
public static class UserMessages
{
    // ── General ─────────────────────────────────────────────────────────────
    public const string NotFound          = "The requested record was not found.";
    public const string Unauthorized      = "You are not authorized to perform this action.";
    public const string Forbidden         = "You don't have permission for this action.";
    public const string ServerError       = "Something went wrong on our end. Please try again.";
    public const string NetworkError      = "Could not connect to the server. Please check your connection and try again.";
    public const string ValidationFailed  = "Please check your input and try again.";
    public const string Conflict          = "This action conflicts with existing data.";

    // ── Patients ─────────────────────────────────────────────────────────────
    public const string PatientNotFound   = "Patient not found. Please verify they are registered in the Patient Registry.";

    // ── IPD ──────────────────────────────────────────────────────────────────
    public const string PatientAlreadyAdmitted = "This patient is already admitted. Please discharge them before creating a new admission.";
    public const string BedAlreadyOccupied     = "This bed is already occupied. Please select a different bed.";
    public const string IpdNotFound            = "IPD record not found.";
    public const string DischargeAlreadyDone   = "This patient has already been discharged.";

    // ── OPD ──────────────────────────────────────────────────────────────────
    public const string OpdNotFound = "OPD record not found.";

    // ── Wards ────────────────────────────────────────────────────────────────
    public const string WardNotFound   = "Ward not found.";
    public const string WardNameTaken  = "A ward with this name already exists. Please use a different name.";

    // ── Rooms ────────────────────────────────────────────────────────────────
    public const string RoomNotFound      = "Room not found.";
    public const string RoomNumberTaken   = "A room with this number already exists in this ward.";
    public const string RoomHasAdmissions = "This room cannot be removed because it has active admissions.";

    // ── Beds ─────────────────────────────────────────────────────────────────
    public const string BedNotFound    = "Bed not found.";
    public const string BedNumberTaken = "A bed with this number already exists in this room.";
    public const string BedIsOccupied  = "This bed is currently occupied and cannot be removed.";
}
