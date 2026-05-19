using MediatR;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineDosageForms.Queries;

public sealed record GetMedicineDosageFormsQuery(int Page, int PageSize, string? Search) : IRequest<PagedResult<MedicineDosageFormDto>>;
