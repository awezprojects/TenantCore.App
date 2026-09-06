using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Commands;

public sealed record UpdateMedicineBundleCommand(Guid Id, Guid ApplicationId, UpdateMedicineBundleDto Request) : IRequest<MedicineBundleDto>;
