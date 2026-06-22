using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class OpdParticularDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid OpdRegistrationId { get; init; }
    public Guid ParticularId { get; init; }
    public string ParticularName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public DateTime? CollectedAt { get; init; }
    public Guid? CounterSessionId { get; init; }
}

public sealed record AddOpdParticularRequest
{
    public Guid OpdRegistrationId { get; init; }
    public Guid ParticularId { get; init; }
    public decimal Amount { get; init; }
}

public sealed record UpdateOpdParticularRequest
{
    public decimal Amount { get; init; }
}

public sealed record CollectOpdParticularRequest
{
    public Guid? CounterSessionId { get; init; }
}

public sealed record CollectAllOpdParticularsRequest
{
    public Guid OpdRegistrationId { get; init; }
    public Guid? CounterSessionId { get; init; }
}
