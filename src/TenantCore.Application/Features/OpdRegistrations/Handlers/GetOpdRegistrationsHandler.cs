using MediatR;
using TenantCore.Application.Features.OpdRegistrations.Queries;
using TenantCore.Application.Features.OpdRegistrations.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdRegistrations.Handlers;

public sealed class GetOpdRegistrationsHandler(
    IOpdRegistrationRepository repository,
    IOpdPaymentRepository paymentRepository)
    : IRequestHandler<GetOpdRegistrationsQuery, PagedResult<OpdRegistrationDto>>
{
    public async Task<PagedResult<OpdRegistrationDto>> Handle(
        GetOpdRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var (items, total) = await repository.GetPagedAsync(
            request.ApplicationId, request.Page, pageSize, request.Search,
            request.DoctorUserId, request.TodayOnly,
            request.FromDate, request.ToDate,
            request.StatusFilter, request.NotVisited, cancellationToken);

        var ids = items.Select(r => r.Id).ToList();
        var payments = await paymentRepository.GetByOpdRegistrationIdsAsync(ids, request.ApplicationId, cancellationToken);
        var paymentByOpdId = payments.ToDictionary(p => p.OpdRegistrationId);

        return new PagedResult<OpdRegistrationDto>
        {
            Items = items.Select(r => OpdRegistrationTranslator.ToDto(
                r, paymentByOpdId.GetValueOrDefault(r.Id))).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = pageSize
        };
    }
}
