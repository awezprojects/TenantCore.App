namespace TenantCore.Shared.Dtos;

public sealed record CreatePrescriptionDto(
    Guid OpdRegistrationId,
    Guid DoctorUserId,
    string DoctorName,
    DateTime? NextVisitDate,
    string? Notes,
    IReadOnlyList<CreatePrescriptionItemDto> Items);
