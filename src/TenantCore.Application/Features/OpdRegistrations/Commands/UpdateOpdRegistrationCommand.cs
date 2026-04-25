using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdRegistrations.Commands;

public sealed record UpdateOpdRegistrationCommand(
    Guid Id,
    Guid ApplicationId,
    Guid DoctorUserId,
    string DoctorName,
    decimal Fee,
    OpdStatus Status,
    string? Notes) : IRequest<OpdRegistrationDto>;
