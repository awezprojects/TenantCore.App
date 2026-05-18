using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Queries;

public sealed record GetPrescriptionByIdQuery(Guid Id, Guid ApplicationId) : IRequest<PrescriptionDto>;
