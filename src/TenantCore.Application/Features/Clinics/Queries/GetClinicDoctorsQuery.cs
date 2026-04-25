using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Clinics.Queries;

public sealed record GetClinicDoctorsQuery(Guid ApplicationId) : IRequest<IEnumerable<DoctorDto>>;
