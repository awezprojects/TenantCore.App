using MediatR;

namespace TenantCore.Application.Features.MedicineBundles.Commands;

public sealed record DeleteMedicineBundleCommand(Guid Id, Guid ApplicationId) : IRequest;
