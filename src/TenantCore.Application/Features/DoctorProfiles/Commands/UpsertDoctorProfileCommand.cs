using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Commands;

public sealed record UpsertDoctorProfileCommand(
    Guid UserId,
    string RegistrationNumber,
    Guid SpecialityId,
    string? QualificationDetails) : IRequest<DoctorProfileDto>;
