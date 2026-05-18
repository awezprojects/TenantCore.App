namespace TenantCore.Shared.Dtos;

public sealed record UpdatePrescriptionDto(
    DateTime? NextVisitDate,
    string? Notes,
    IReadOnlyList<CreatePrescriptionItemDto> Items);
