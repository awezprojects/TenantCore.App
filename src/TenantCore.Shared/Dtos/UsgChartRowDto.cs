namespace TenantCore.Shared.Dtos;

public record UsgChartRowDto
{
    public int RowOrder { get; init; }
    public string WeekLabel { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public string Activity { get; init; } = string.Empty;
    public string Indication { get; init; } = string.Empty;
}
