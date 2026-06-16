using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public record PregnancyTenureSummaryDto
{
    public Guid TenureId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientPhone { get; init; } = string.Empty;
    public string PatientFullName { get; init; } = string.Empty;
    public DateOnly Lmp { get; init; }
    public DateOnly EddByLmp { get; init; }
    public DateOnly? EddByUsg { get; init; }
    public DateOnly EffectiveEdd { get; init; }
    public PregnancyTenureStatus Status { get; init; }
    public int DaysOverdue { get; init; }
}
