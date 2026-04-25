using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record RegisterPatientCommand(
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
    bool ShowFullAadhaar = false) : IRequest<PatientDto>;
