using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Commands;

public sealed record EditApplicationCommand(
    Guid ApplicationId,
    ApplicationCreationRequestDto Request) : IRequest<ApplicationResponseDto>;
