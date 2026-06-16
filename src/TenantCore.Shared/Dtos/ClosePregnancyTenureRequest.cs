using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public record ClosePregnancyTenureRequest
{
    public PregnancyOutcome Outcome { get; init; }
    public string? Notes { get; init; }
}
