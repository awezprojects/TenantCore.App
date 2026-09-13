using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.DosageRemarks.Commands;

public sealed record DeleteDosageRemarkCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Dosage Remark Deletion";
}
