using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Queries;

public sealed record GetMedicineBundlesQuery(Guid ApplicationId) : IRequest<IEnumerable<MedicineBundleDto>>;
