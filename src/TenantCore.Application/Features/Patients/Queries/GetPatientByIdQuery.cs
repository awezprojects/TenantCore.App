using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Queries;

public sealed record GetPatientByIdQuery(Guid Id, Guid ApplicationId, bool ShowFullAadhaar = false) : IRequest<PatientDto>;
