using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class OpdRegistrationDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string RegistrationNumber { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
    public decimal Fee { get; init; }
    public OpdStatus Status { get; init; }
    public string? Notes { get; init; }
    public decimal? Weight { get; init; }
    public string? BloodPressure { get; init; }
    public int? PulseRate { get; init; }
    public decimal? OxygenSaturation { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateOpdRegistrationDto(
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    decimal? Fee,
    string? Notes,
    decimal? Weight = null,
    string? BloodPressure = null,
    int? PulseRate = null,
    decimal? OxygenSaturation = null);

public sealed record UpdateOpdRegistrationDto(
    Guid DoctorUserId,
    string DoctorName,
    decimal Fee,
    OpdStatus Status,
    string? Notes);
