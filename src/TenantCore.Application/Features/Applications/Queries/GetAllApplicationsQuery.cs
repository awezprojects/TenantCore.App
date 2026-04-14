using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public record GetAllApplicationsQuery : IRequest<List<ApplicationResponseDto>>;
