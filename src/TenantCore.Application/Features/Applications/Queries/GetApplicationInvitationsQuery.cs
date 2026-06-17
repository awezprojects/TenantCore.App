using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public sealed record GetApplicationInvitationsQuery(Guid ApplicationId) : IRequest<List<InvitationResponseDto>>;
