using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.VitalPresets.Commands;

public sealed record DeleteVitalPresetCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Vital Preset Deletion";
}
