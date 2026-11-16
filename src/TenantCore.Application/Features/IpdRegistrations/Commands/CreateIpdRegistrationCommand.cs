using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record CreateIpdRegistrationCommand(
    Guid ApplicationId,
    Guid PatientId,
    Guid DoctorUserId,
    string DoctorName,
    Guid? BedId,
    decimal InitialFee,
    string? AdmissionNotes) : IRequest<IpdRegistrationDto>;
