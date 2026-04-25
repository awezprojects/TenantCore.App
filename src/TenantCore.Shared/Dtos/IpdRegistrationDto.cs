using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class IpdRegistrationDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid DoctorUserId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string AdmissionNumber { get; init; } = string.Empty;
    public DateTime AdmissionDate { get; init; }
    public DateTime? DischargeDate { get; init; }
    public string? WardName { get; init; }
    public string? RoomNumber { get; init; }
    public string? BedNumber { get; init; }
    public decimal InitialFee { get; init; }
    public IpdStatus Status { get; init; }
    public string? AdmissionNotes { get; init; }
    public string? DischargeNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateIpdRegistrationDto(
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    decimal InitialFee,
    string? AdmissionNotes);

public sealed record UpdateIpdRegistrationDto(
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    IpdStatus Status,
    string? AdmissionNotes);

public sealed record DischargePatientDto(string? DischargeNotes);
