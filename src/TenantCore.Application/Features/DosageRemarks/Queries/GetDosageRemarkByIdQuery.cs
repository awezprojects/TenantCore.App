using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Queries;

public sealed record GetDosageRemarkByIdQuery(Guid Id, Guid ApplicationId) : IRequest<DosageRemarkDto>;
