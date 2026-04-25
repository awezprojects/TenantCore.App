using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Applications.Queries;

public sealed record GetDoctorsByApplicationQuery(Guid ApplicationId) : IRequest<List<DoctorDto>>;
