namespace TenantCore.Shared.Dtos;

public sealed record CreatePrescriptionDto(
    Guid OpdRegistrationId,
    Guid DoctorUserId,
    string DoctorName,
    DateTime? NextVisitDate,
    string? Diagnosis,
    IReadOnlyList<string> Investigations,
    string? Notes,
    string? VitalBP,
    int? VitalPulse,
    decimal? VitalTemp,
    decimal? VitalWeight,
    decimal? VitalSpO2,
    int? VitalRR,
    decimal? VitalSugar,
    IReadOnlyList<CreatePrescriptionItemDto> Items,
    UpsertObstetricPrescriptionDataDto? ObstetricData = null);
