using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ClinicFeatureFlags : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public bool PrepaidOpdEnabled { get; private set; }

    private ClinicFeatureFlags() { }

    public static ClinicFeatureFlags Create(Guid applicationId, bool prepaidOpdEnabled = true) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        PrepaidOpdEnabled = prepaidOpdEnabled,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(bool prepaidOpdEnabled)
    {
        PrepaidOpdEnabled = prepaidOpdEnabled;
        SetUpdatedAt();
    }
}
