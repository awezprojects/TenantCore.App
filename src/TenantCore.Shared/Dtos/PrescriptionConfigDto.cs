using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PrescriptionConfigDto
{
    public Guid ApplicationId { get; init; }
    public PrescriptionLanguage DefaultLanguage { get; init; }
    public int PrintMarginTop { get; init; }
    public int PrintMarginRight { get; init; }
    public int PrintMarginBottom { get; init; }
    public int PrintMarginLeft { get; init; }
    public bool HideClinicHeader { get; init; }
}
