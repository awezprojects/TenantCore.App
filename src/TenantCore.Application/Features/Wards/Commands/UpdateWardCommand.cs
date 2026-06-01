using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Commands;

public sealed record UpdateWardCommand(Guid Id, Guid ApplicationId, string Name, string? Description) : IRequest<WardDto>;
