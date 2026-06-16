namespace TenantCore.Shared.Dtos;

public record ClinicUsgTemplateDto
{
    public Guid ApplicationId { get; init; }
    public bool IsCustomized { get; init; }
    public IReadOnlyList<UsgTemplateRowDto> Rows { get; init; } = [];
}
