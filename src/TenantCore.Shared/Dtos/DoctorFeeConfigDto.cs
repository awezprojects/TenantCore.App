namespace TenantCore.Shared.Dtos;

public class DoctorFeeConfigDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public decimal VisitFee { get; init; }
    public bool IsActive { get; init; }
}

public class DoctorFeeConfigSummaryDto
{
    public Guid Id { get; init; }
    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public decimal VisitFee { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateDoctorFeeConfigRequest
{
    public Guid DoctorProfileId { get; init; }
    public decimal VisitFee { get; init; }
}

public sealed record UpdateDoctorFeeConfigRequest
{
    public decimal VisitFee { get; init; }
    public bool IsActive { get; init; }
}
