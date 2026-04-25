using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ClinicFeeConfig : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public decimal OpdFee { get; private set; }

    private ClinicFeeConfig() { }

    public static ClinicFeeConfig Create(Guid applicationId, decimal opdFee) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        OpdFee = opdFee,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(decimal opdFee)
    {
        OpdFee = opdFee;
        SetUpdatedAt();
    }
}
