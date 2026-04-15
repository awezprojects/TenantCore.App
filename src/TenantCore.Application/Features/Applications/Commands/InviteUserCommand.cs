using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Commands;

public record InviteUserCommand(
    Guid InvitedBy,
    InviteUserRequestDto Request) : IRequest<InvitationResponseDto>;
