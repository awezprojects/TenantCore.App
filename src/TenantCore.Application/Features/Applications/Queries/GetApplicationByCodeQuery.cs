using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public record GetApplicationByCodeQuery(string Code) : IRequest<ApplicationResponseDto?>;
