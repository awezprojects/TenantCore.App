using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

// Status and bed move only through their own dedicated commands (Discharge, TransferBed,
// Cancel) — see DEF-IPD-04 in the IPD E2E test report for why an open PUT was a trap door.
public sealed record UpdateIpdRegistrationCommand(
    Guid Id,
    Guid ApplicationId,
    Guid DoctorUserId,
    string DoctorName,
    string? AdmissionNotes) : IRequest<IpdRegistrationDto>;
