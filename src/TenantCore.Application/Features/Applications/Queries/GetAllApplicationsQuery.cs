using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public sealed record GetAllApplicationsQuery : IRequest<List<ApplicationResponseDto>>;
