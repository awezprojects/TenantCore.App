using MediatR;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Features.Applications.Queries;

public sealed record GetClinicStaffDirectoryQuery(Guid ApplicationId, List<string>? Roles) : IRequest<List<ClinicStaffDirectoryDto>>;
