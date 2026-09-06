using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.MedicineBundles.Commands;

public sealed record CreateMedicineBundleCommand(Guid ApplicationId, CreateMedicineBundleDto Request) : IRequest<MedicineBundleDto>;
