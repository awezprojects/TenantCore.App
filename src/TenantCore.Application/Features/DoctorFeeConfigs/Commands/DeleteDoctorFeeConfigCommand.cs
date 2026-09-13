using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Commands;

public sealed record DeleteDoctorFeeConfigCommand(Guid Id, Guid ApplicationId) : IRequest, IBusinessAction
{
    public string ActionName => "Doctor Fee Config Deletion";
}
