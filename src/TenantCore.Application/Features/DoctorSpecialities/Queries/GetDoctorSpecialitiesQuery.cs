using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorSpecialities.Queries;

public sealed record GetDoctorSpecialitiesQuery : IRequest<List<DoctorSpecialityDto>>;
