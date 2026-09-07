namespace TenantCore.Shared.Enums;

// A doctor's personal choice of print layout for their prescriptions. This is a per-doctor
// preference (stored on DoctorProfile), never a clinic-wide setting — two doctors in the same
// clinic can each print in a different style without affecting one another.
public enum PrescriptionTemplate
{
    Classic = 1,
    Compact = 2,
    Modern = 3,
    Minimal = 4,
    TwoColumn = 5
}
