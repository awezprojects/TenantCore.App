using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Queries;

public sealed record GetMedicineBundleByIdQuery(Guid Id, Guid ApplicationId) : IRequest<MedicineBundleDto>;
