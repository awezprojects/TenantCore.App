namespace TenantCore.Shared.Dtos;

public record UsgTemplateRowDto
{
    public int RowOrder { get; init; }
    public string WeekLabel { get; init; } = string.Empty;
    public int LmpDayOffset { get; init; }
    public string Activity { get; init; } = string.Empty;
    public string Indication { get; init; } = string.Empty;
}
