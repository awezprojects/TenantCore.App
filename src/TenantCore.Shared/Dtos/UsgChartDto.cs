namespace TenantCore.Shared.Dtos;

public record UsgChartDto
{
    public Guid PatientId { get; init; }
    public DateOnly? Lmp { get; init; }
    public DateOnly? EddByLmp { get; init; }
    public DateOnly? EddByUsg { get; init; }
    public IReadOnlyList<UsgChartRowDto> Rows { get; init; } = [];
}
