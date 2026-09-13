using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record DeletePatientCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Patient Deactivation";
}
