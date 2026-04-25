using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Patients.Queries;

public sealed record GetPatientsQuery(
    Guid ApplicationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool ShowFullAadhaar = false) : IRequest<PagedResult<PatientDto>>;
