namespace TenantCore.Shared.Dtos;

public record UpsertClinicUsgTemplateRequest
{
    public List<UsgTemplateRowDto> Rows { get; init; } = [];
}
