namespace TenantCore.Shared.Dtos;

public class SessionCollectionDto
{
    public Guid OpdRegistrationId { get; init; }
    public string RegistrationNumber { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public decimal ConsultationFee { get; init; }
    public decimal ItemsTotal { get; init; }
    public decimal TotalCollected { get; init; }
    public bool HasItems { get; init; }
    public DateTime? CollectedAt { get; init; }

    // DEF-MR-01: with two receptionists sharing one clinic-wide session, the Counter page had no
    // way to show who actually took a given patient's cash.
    public Guid? CollectedByUserId { get; init; }
}
