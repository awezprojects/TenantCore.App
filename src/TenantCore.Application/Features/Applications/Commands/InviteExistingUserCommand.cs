using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Commands;

public record InviteExistingUserCommand(
    Guid InvitedBy,
    InviteExistingUserRequestDto Request) : IRequest;
