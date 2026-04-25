using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Commands;

public sealed record CreateOpdRegistrationCommand(
    Guid ApplicationId,
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    decimal? Fee,
    string? Notes,
    decimal? Weight,
    string? BloodPressure,
    int? PulseRate,
    decimal? OxygenSaturation) : IRequest<OpdRegistrationDto>;
