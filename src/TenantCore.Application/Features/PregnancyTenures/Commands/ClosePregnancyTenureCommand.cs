using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Commands;

public sealed record ClosePregnancyTenureCommand(
    Guid TenureId,
    ClosePregnancyTenureRequest Request,
    Guid ApplicationId) : IRequest<PregnancyTenureDto>;
