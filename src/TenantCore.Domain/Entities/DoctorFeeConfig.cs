using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class DoctorFeeConfig : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public decimal VisitFee { get; private set; }
    public bool IsActive { get; private set; }

    private DoctorFeeConfig() { }

    public static DoctorFeeConfig Create(Guid applicationId, Guid doctorProfileId, decimal visitFee) => new()
    {
        ApplicationId = applicationId,
        DoctorProfileId = doctorProfileId,
        VisitFee = visitFee,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(decimal visitFee, bool isActive)
    {
        VisitFee = visitFee;
        IsActive = isActive;
        SetUpdatedAt();
    }
}
