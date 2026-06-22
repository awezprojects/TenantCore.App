using MediatR;

namespace TenantCore.Application.Features.Particulars.Commands;

public sealed record DeleteParticularCommand(Guid Id, Guid ApplicationId) : IRequest;
