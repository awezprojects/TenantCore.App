using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Commands;

public sealed record CreateParticularCommand(CreateParticularRequest Request, Guid ApplicationId) : IRequest<Guid>;
