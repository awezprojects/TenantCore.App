using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PrescriptionDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid OpdRegistrationId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientEmail { get; init; }
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string PrescriptionNumber { get; init; } = string.Empty;
    public DateTime PrescribedDate { get; init; }
    public DateTime? NextVisitDate { get; init; }
    public string? Diagnosis { get; init; }
    public IReadOnlyList<string> Investigations { get; init; } = [];
    public string? Notes { get; init; }
    public string? VitalBP { get; init; }
    public int? VitalPulse { get; init; }
    public decimal? VitalTemp { get; init; }
    public decimal? VitalWeight { get; init; }
    public decimal? VitalSpO2 { get; init; }
    public int? VitalRR { get; init; }
    public decimal? VitalSugar { get; init; }
    public PrescriptionStatus Status { get; init; }
    public bool IsEmailSent { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<PrescriptionItemDto> Items { get; init; } = [];
    public IReadOnlyList<PrescriptionReportDto> Reports { get; init; } = [];

    // Specialty-specific data (null for non-matching specialties)
    public ObstetricPrescriptionDataDto? ObstetricData { get; init; }
}
