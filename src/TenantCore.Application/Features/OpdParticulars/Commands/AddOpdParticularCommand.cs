using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Commands;

public sealed record AddOpdParticularCommand(AddOpdParticularRequest Request, Guid ApplicationId) : IRequest<OpdParticularDto>;
