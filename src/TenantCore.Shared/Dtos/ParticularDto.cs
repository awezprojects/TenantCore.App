namespace TenantCore.Shared.Dtos;

public class ParticularDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal DefaultAmount { get; init; }
    public bool IsActive { get; init; }
}

public class ParticularSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal DefaultAmount { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateParticularRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal DefaultAmount { get; init; }
}

public sealed record UpdateParticularRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal DefaultAmount { get; init; }
    public bool IsActive { get; init; }
}
