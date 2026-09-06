namespace TenantCore.Shared.Dtos;

public sealed record UpdateMedicineBundleDto(
    string Name,
    int DurationDays,
    string? Notes,
    IReadOnlyList<CreateMedicineBundleItemDto> Items);
