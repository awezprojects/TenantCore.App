using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.PrescriptionConfig.Commands;

public sealed record UpsertPrescriptionConfigCommand(
    Guid ApplicationId,
    PrescriptionLanguage DefaultLanguage) : IRequest<PrescriptionConfigDto>;
