using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.VitalPresets.Commands;

public sealed record AddVitalPresetCommand(Guid ApplicationId, VitalFieldType VitalField, string Value) : IRequest<VitalPresetLookupItemDto>;
