using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public record PregnancyTenureDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public Guid ApplicationId { get; init; }
    public DateOnly Lmp { get; init; }
    public DateOnly EddByLmp { get; init; }
    public DateOnly? EddByUsg { get; init; }
    public DateOnly EffectiveEdd { get; init; }
    public PregnancyTenureStatus Status { get; init; }
    public PregnancyOutcome? Outcome { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
