using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Commands;

public sealed record CancelIpdRegistrationCommand(
    Guid Id,
    Guid ApplicationId,
    string? Reason,
    Guid? CancelledByUserId = null) : IRequest<IpdRegistrationDto>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={Id}";
}
