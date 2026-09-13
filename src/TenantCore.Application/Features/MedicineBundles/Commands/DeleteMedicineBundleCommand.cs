using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.MedicineBundles.Commands;

public sealed record DeleteMedicineBundleCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Medicine Bundle Deletion";
}
