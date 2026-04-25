namespace TenantCore.Shared.Dtos;

public class ClinicFeeConfigDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public decimal OpdFee { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record UpdateClinicFeeConfigDto(decimal OpdFee);
