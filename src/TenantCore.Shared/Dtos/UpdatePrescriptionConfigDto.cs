using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record UpdatePrescriptionConfigDto(
    PrescriptionLanguage DefaultLanguage,
    int PrintMarginTop = 0,
    int PrintMarginRight = 0,
    int PrintMarginBottom = 0,
    int PrintMarginLeft = 0,
    bool HideClinicHeader = false);
