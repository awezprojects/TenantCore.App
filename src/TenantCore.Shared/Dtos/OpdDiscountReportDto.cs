namespace TenantCore.Shared.Dtos;

public class OpdDiscountReportDto
{
    public IReadOnlyList<OpdDiscountEntryDto> Discounts { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class OpdDiscountEntryDto
{
    public Guid OpdRegistrationId { get; init; }
    public string RegistrationNumber { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public Guid? DiscountedByUserId { get; init; }
    public DateTime? DiscountedAt { get; init; }
}
