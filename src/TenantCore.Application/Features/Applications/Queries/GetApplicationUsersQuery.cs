using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public record GetApplicationUsersQuery(Guid ApplicationId) : IRequest<List<ApplicationUserResponseDto>>;
