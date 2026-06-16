using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Queries;

public sealed record GetPatientPregnancyTenuresQuery(Guid PatientId, Guid ApplicationId)
    : IRequest<IEnumerable<PregnancyTenureDto>>;
