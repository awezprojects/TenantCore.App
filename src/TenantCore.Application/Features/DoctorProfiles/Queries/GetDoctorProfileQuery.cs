using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Queries;

public sealed record GetDoctorProfileQuery(Guid UserId) : IRequest<DoctorProfileDto?>;
