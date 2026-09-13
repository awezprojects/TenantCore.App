using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record SubmitPrescriptionCommand(Guid Id, Guid ApplicationId) : IRequest<PrescriptionDto>, IBusinessAction
{
    public string ActionName => "Prescription Submission";
}
