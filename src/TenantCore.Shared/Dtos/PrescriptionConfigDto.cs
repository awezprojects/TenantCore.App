using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PrescriptionConfigDto
{
    public Guid ApplicationId { get; init; }
    public PrescriptionLanguage DefaultLanguage { get; init; }
}
