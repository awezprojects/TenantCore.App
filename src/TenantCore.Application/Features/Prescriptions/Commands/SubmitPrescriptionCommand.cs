using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record SubmitPrescriptionCommand(Guid Id, Guid ApplicationId) : IRequest<PrescriptionDto>;
