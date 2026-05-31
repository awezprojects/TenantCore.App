using System.ComponentModel.DataAnnotations;

namespace TenantCore.Shared.Dtos;

public class DoctorProfileDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string RegistrationNumber { get; init; } = string.Empty;
    public bool IsRegistrationVerified { get; init; }
    public string? Specialty { get; init; }
    public string? QualificationDetails { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UpsertDoctorProfileDto
{
    [Required(ErrorMessage = "Registration number is required")]
    [MaxLength(100, ErrorMessage = "Registration number cannot exceed 100 characters")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Specialty cannot exceed 200 characters")]
    public string? Specialty { get; set; }

    [MaxLength(500, ErrorMessage = "Qualification details cannot exceed 500 characters")]
    public string? QualificationDetails { get; set; }
}
