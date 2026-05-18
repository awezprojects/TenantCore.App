using MediatR;

namespace TenantCore.Application.Features.DosageRemarks.Commands;

public sealed record DeleteDosageRemarkCommand(Guid Id, Guid ApplicationId) : IRequest;
