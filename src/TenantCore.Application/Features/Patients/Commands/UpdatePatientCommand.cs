using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record UpdatePatientCommand(
    Guid Id,
    Guid ApplicationId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string PhoneNumber,
    string? Email,
    string? AadhaarNumber,
    string? PhotoUrl,
    string? Address,
    string? BloodGroup = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    string? KnownAllergies = null,
    string? MedicalHistory = null,
    bool ShowFullAadhaar = false) : IRequest<PatientDto>;
