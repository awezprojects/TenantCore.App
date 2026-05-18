using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class PrescriptionConfig : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public PrescriptionLanguage DefaultLanguage { get; private set; }

    private PrescriptionConfig() { }

    public static PrescriptionConfig Create(Guid applicationId, PrescriptionLanguage defaultLanguage) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        DefaultLanguage = defaultLanguage,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(PrescriptionLanguage defaultLanguage)
    {
        DefaultLanguage = defaultLanguage;
        SetUpdatedAt();
    }
}
