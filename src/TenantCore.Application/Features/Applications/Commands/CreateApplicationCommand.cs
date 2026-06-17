using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record CreateApplicationCommand(
    Guid OwnerId,
    ApplicationCreationRequestDto Request) : IRequest<ApplicationResponseDto>;
