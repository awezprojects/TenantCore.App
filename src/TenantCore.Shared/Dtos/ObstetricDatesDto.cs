namespace TenantCore.Shared.Dtos;

public record ObstetricDatesDto
{
    public Guid Id { get; init; }
    public Guid PrescriptionId { get; init; }
    public DateOnly? Lmp { get; init; }
    public DateOnly? EddByLmp { get; init; }
    public DateOnly? EddByUsg { get; init; }
}
