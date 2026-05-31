using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Queries;

public sealed record GetOpdRegistrationsQuery(
    Guid ApplicationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? DoctorUserId = null,
    bool TodayOnly = false,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    TenantCore.Shared.Enums.OpdStatus? StatusFilter = null,
    bool NotVisited = false) : IRequest<PagedResult<OpdRegistrationDto>>;
