namespace TenantCore.Shared.Dtos;

public class DoctorWiseCollectionDto
{
    public IReadOnlyList<DoctorWiseTotalDto> Doctors { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class DoctorWiseTotalDto
{
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}
