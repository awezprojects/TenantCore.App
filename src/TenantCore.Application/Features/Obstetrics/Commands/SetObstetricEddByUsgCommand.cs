using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Commands;

public sealed record SetObstetricEddByUsgCommand(Guid PrescriptionId, SetEddByUsgRequest Request, Guid ApplicationId) : IRequest<ObstetricDatesDto>;
