using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PrescriptionConfig.Queries;

public sealed record GetPrescriptionConfigQuery(Guid ApplicationId) : IRequest<PrescriptionConfigDto>;
