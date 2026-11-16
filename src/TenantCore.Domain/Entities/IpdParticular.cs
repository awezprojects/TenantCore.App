using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

// Mirrors OpdParticular's role (a clinic-defined Particular applied to an admission) but
// without per-item collection tracking: unlike OPD's pay-as-you-go flow, an IPD bill is
// settled once, at discharge, so there is no per-item "collected" state to carry here.
public class IpdParticular : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid IpdRegistrationId { get; private set; }
    public Guid ParticularId { get; private set; }
    public string ParticularName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public Guid AddedByUserId { get; private set; }

    private IpdParticular() { }

    public static IpdParticular Create(
        Guid applicationId,
        Guid ipdRegistrationId,
        Guid particularId,
        string particularName,
        decimal amount,
        Guid addedByUserId) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        IpdRegistrationId = ipdRegistrationId,
        ParticularId = particularId,
        ParticularName = particularName,
        Amount = amount,
        AddedByUserId = addedByUserId,
        CreatedAt = DateTime.UtcNow
    };
}
