using MediatR;

namespace TenantCore.Application.Features.Wards.Commands;

public sealed record DeleteWardCommand(Guid Id, Guid ApplicationId) : IRequest;
