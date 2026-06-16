using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Commands;

public sealed record SetObstetricLmpCommand(Guid PrescriptionId, SetLmpRequest Request, Guid ApplicationId) : IRequest<ObstetricDatesDto>;
