using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record CreatePrescriptionCommand(
    Guid ApplicationId,
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
    UpsertObstetricPrescriptionDataDto? ObstetricData = null) : IRequest<PrescriptionDto>;
