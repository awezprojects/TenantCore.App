using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto?>;
