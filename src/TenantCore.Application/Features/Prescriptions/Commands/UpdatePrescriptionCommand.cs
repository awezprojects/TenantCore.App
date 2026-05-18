using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record UpdatePrescriptionCommand(
    Guid Id,
    Guid ApplicationId,
    DateTime? NextVisitDate,
    string? Notes,
    IReadOnlyList<CreatePrescriptionItemDto> Items) : IRequest<PrescriptionDto>;
