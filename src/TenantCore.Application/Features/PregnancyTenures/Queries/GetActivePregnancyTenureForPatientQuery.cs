using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Queries;

public sealed record GetActivePregnancyTenureForPatientQuery(
    Guid PatientId,
    Guid ApplicationId) : IRequest<PregnancyTenureDto?>;
