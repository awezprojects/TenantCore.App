using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record CreatePrescriptionCommand(
    Guid ApplicationId,
    Guid OpdRegistrationId,
    Guid DoctorUserId,
    string DoctorName,
    DateTime? NextVisitDate,
    string? Notes,
    IReadOnlyList<CreatePrescriptionItemDto> Items) : IRequest<PrescriptionDto>;
