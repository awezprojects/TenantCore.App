using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class PrescriptionConfig : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public PrescriptionLanguage DefaultLanguage { get; private set; }

    public int PrintMarginTop { get; private set; }
    public int PrintMarginRight { get; private set; }
    public int PrintMarginBottom { get; private set; }
    public int PrintMarginLeft { get; private set; }
    public bool HideClinicHeader { get; private set; }

    private PrescriptionConfig() { }

    public static PrescriptionConfig Create(
        Guid applicationId,
        PrescriptionLanguage defaultLanguage,
        int printMarginTop = 0, int printMarginRight = 0,
        int printMarginBottom = 0, int printMarginLeft = 0,
        bool hideClinicHeader = false) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        DefaultLanguage = defaultLanguage,
        PrintMarginTop = printMarginTop,
        PrintMarginRight = printMarginRight,
        PrintMarginBottom = printMarginBottom,
        PrintMarginLeft = printMarginLeft,
        HideClinicHeader = hideClinicHeader,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        PrescriptionLanguage defaultLanguage,
        int printMarginTop, int printMarginRight,
        int printMarginBottom, int printMarginLeft,
        bool hideClinicHeader)
    {
        DefaultLanguage = defaultLanguage;
        PrintMarginTop = printMarginTop;
        PrintMarginRight = printMarginRight;
        PrintMarginBottom = printMarginBottom;
        PrintMarginLeft = printMarginLeft;
        HideClinicHeader = hideClinicHeader;
        SetUpdatedAt();
    }
}
