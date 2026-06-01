using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Commands;

public sealed record CreateWardCommand(Guid ApplicationId, string Name, string? Description) : IRequest<WardDto>;
