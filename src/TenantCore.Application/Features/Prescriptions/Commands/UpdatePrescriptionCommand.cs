using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record UpdatePrescriptionCommand(
    Guid Id,
    Guid ApplicationId,
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
