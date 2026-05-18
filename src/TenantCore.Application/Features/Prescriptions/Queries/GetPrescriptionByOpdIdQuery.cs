using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Queries;

public sealed record GetPrescriptionByOpdIdQuery(Guid OpdRegistrationId, Guid ApplicationId) : IRequest<PrescriptionDto>;
