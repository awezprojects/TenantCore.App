using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class PatientDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateOnly? DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? AadhaarNumber { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record CreatePatientDto(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address);

public sealed record UpdatePatientDto(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address);
