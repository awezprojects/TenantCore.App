using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class PregnancyTenure : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Guid ApplicationId { get; set; }
    public DateOnly Lmp { get; set; }
    public DateOnly EddByLmp { get; set; }
    public DateOnly? EddByUsg { get; set; }
    public PregnancyTenureStatus Status { get; set; } = PregnancyTenureStatus.Active;
    public PregnancyOutcome? Outcome { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }

    public Patient Patient { get; set; } = null!;
}
