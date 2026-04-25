using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record UpdateIpdRegistrationCommand(
    Guid Id,
    Guid ApplicationId,
    Guid DoctorUserId,
    string DoctorName,
    string? WardName,
    string? RoomNumber,
    string? BedNumber,
    IpdStatus Status,
    string? AdmissionNotes) : IRequest<IpdRegistrationDto>;
