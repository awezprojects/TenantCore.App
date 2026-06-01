using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Queries;

public sealed record GetWardByIdQuery(Guid Id, Guid ApplicationId) : IRequest<WardDto>;
