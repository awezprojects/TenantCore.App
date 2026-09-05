using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ClinicLocation : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid StateId { get; private set; }
    public Guid CityId { get; private set; }

    public State State { get; private set; } = null!;
    public City City { get; private set; } = null!;

    private ClinicLocation() { }

    public static ClinicLocation Create(Guid applicationId, Guid stateId, Guid cityId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        StateId = stateId,
        CityId = cityId,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(Guid stateId, Guid cityId)
    {
        StateId = stateId;
        CityId = cityId;
        SetUpdatedAt();
    }
}
