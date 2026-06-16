using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Queries;

public sealed record GetObstetricDatesQuery(Guid PrescriptionId, Guid ApplicationId) : IRequest<ObstetricDatesDto>;
