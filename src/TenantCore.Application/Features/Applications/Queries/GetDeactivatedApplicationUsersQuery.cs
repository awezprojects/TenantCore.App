using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public record GetDeactivatedApplicationUsersQuery(Guid ApplicationId) : IRequest<List<ApplicationUserResponseDto>>;
