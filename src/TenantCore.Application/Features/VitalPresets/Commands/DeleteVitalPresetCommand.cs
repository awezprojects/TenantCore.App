using MediatR;

namespace TenantCore.Application.Features.VitalPresets.Commands;

public sealed record DeleteVitalPresetCommand(Guid Id, Guid ApplicationId) : IRequest;
