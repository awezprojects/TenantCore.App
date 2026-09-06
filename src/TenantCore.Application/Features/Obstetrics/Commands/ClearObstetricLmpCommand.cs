using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Commands;

public sealed record ClearObstetricLmpCommand(Guid PrescriptionId, Guid ApplicationId) : IRequest<ObstetricDatesDto>;
