using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record DischargePatientCommand(
    Guid Id,
    Guid ApplicationId,
    string? DischargeNotes) : IRequest<IpdRegistrationDto>;
